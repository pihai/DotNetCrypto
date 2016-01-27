// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System.Net.Http
open System.Net
open System
open System.Text
open System.Security.Cryptography
open System.Xml

let port = 8083

let publicKeyBytes = Convert.FromBase64String("RUNTMSAAAACvCjDrBMt8pZGjdy4OpXfj/KEhnzFvRK7097otjloCOoJGCA3upVQBuWB8TAgU5FcY0uSFE8MEmK2HyKrOvrrd")

type LicenseValidationResult =
  | Success of string
  | NoFreeLicense
  | KeySignatureCheckFailed
  | WrongFormat
  | ServerSignatureCheckFailed
  | UnknownStatusCode of HttpStatusCode

let validateLicense (response: string) =
  try
    let lines = response.Split([|System.Environment.NewLine |], System.StringSplitOptions.None)
    if lines.Length = 2 then
      let license = lines.[0]
      let serverSignature = lines.[1]

      let licenseXml = XmlDocument()
      licenseXml.LoadXml license

      let keySig = licenseXml.SelectSingleNode("/license/sig").InnerText
      let keyData = licenseXml.SelectSingleNode("/license/data").InnerXml

      use keySigVerifier = new ECDsaCng(CngKey.Import(publicKeyBytes, CngKeyBlobFormat.EccPublicBlob))
      if keySigVerifier.VerifyData(Encoding.UTF8.GetBytes(keyData), Convert.FromBase64String(keySig)) then
        let serverPubKey = licenseXml.SelectSingleNode("/license/data/pub-key").InnerText
        use serverSigVerifier = new ECDsaCng(CngKey.Import(serverPubKey |> Convert.FromBase64String, CngKeyBlobFormat.EccPublicBlob))
        if serverSigVerifier.VerifyData(Encoding.UTF8.GetBytes(license), serverSignature |> Convert.FromBase64String) then
          Success (licenseXml.SelectSingleNode("/license/data/nr").InnerText)
        else
          ServerSignatureCheckFailed
      else
        KeySignatureCheckFailed
    else
      WrongFormat
  with ex ->
    printfn "%A" ex
    WrongFormat


let requestLicenseToken ipAddress = async {
  use client = new HttpClient()
  let! response = client.GetAsync(sprintf "http://%s:%d/GetLicense" ipAddress port) |> Async.AwaitTask
  if response.IsSuccessStatusCode then
    let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
    return validateLicense content
  elif response.StatusCode = HttpStatusCode.Forbidden then
    return NoFreeLicense
  else
    return UnknownStatusCode response.StatusCode
  }

let releaseLicenseToken ipAddress token = async {
  use client = new HttpClient()
  let! response = 
    client.GetAsync(sprintf "http://%s:%d/ReleaseLicense/%s" ipAddress port token) 
    |> Async.AwaitTask
  if response.IsSuccessStatusCode then
    printfn "successfully released token."
  else
    printfn "failed to release token."
  }

[<EntryPoint>]
let main argv =
  if argv.Length <> 1 then
    printfn "Please provide the ip-address of the license server as first argument!"
    System.Environment.Exit(0)

  let ipAddress = argv.[0]
  let license = requestLicenseToken ipAddress |> Async.RunSynchronously

  match license with
  | Success(key) ->
    try
      printfn "Running..."
      System.Console.Read() |> ignore
    finally
      releaseLicenseToken ipAddress key |> Async.RunSynchronously
  | x -> printfn "Failed due to: %A" x

  Console.Read() |> ignore

  0 // return an integer exit code
