// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System.Net.Http
open System.Net
open System
open System.Text
open System.Security.Cryptography

let port = 8083

let publicKeyBytes = System.Convert.FromBase64String("RUNTMSAAAACvCjDrBMt8pZGjdy4OpXfj/KEhnzFvRK7097otjloCOoJGCA3upVQBuWB8TAgU5FcY0uSFE8MEmK2HyKrOvrrd")

let requestLicenseToken ipAddress = async {
  use client = new HttpClient()
  let! response = client.GetAsync(sprintf "http://%s:%d/GetLicense" ipAddress port) |> Async.AwaitTask
  if response.IsSuccessStatusCode then
    let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
    let lines = content.Split([|System.Environment.NewLine |], System.StringSplitOptions.None)
    if lines.Length = 2 then
      let token = lines.[0]
      let signature = lines.[1]
      use sigVerifier = new ECDsaCng(CngKey.Import(publicKeyBytes, CngKeyBlobFormat.EccPublicBlob))
      if sigVerifier.VerifyData(Encoding.UTF8.GetBytes(token), Convert.FromBase64String(signature)) then
        return Some token
      else
        printfn "Verification failed."
        return None
    else
      return None
  else
    return None
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
  let licenseToken = requestLicenseToken ipAddress |> Async.RunSynchronously

  if licenseToken.IsSome then
    try
      printfn "Running..."
      System.Console.Read() |> ignore
    finally
      releaseLicenseToken ipAddress licenseToken.Value |> Async.RunSynchronously
  else
    printfn "Got no license."
    System.Console.Read() |> ignore

  0 // return an integer exit code
