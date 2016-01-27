open Suave
open Suave.Http
open Suave.Web
open Suave.Operators
open System.IO
open System.Xml
open FSharp.Data
open Suave.Successful
open Suave.Filters
open System.Security.Cryptography
open System
open System.Text
open System.Windows.Forms

let port = 80
let private keyName = "LicenseServerKey"

type private Commands = 
  | RequestLicense of AsyncReplyChannel<string option>
  | ReleaseLicense of string * AsyncReplyChannel<bool>
  | GetLicenseUsage of AsyncReplyChannel<int * int>

let loadLicensesFromFile fileName =
  if File.Exists(fileName) then
    File.ReadAllLines(fileName)
    |> Seq.map (fun license ->
      let doc = XmlDocument()
      doc.LoadXml license
      let nr = doc.SelectSingleNode("//nr").InnerXml
      nr, license
    )
    |> Seq.toList
  else
    []

[<EntryPoint>]
[<STAThread>]
let main argv =
  let key =
    if not (CngKey.Exists(keyName)) then
      CngKey.Create(CngAlgorithm.ECDsaP256, keyName)
    else
      CngKey.Open(keyName)

  let licenses = loadLicensesFromFile "keys.txt"

  printfn "Offering %d licenses." licenses.Length

  let licenseManager = MailboxProcessor.Start(fun inbox ->

    let rec loop usedKeys = async {
      let! msg = inbox.Receive()
      match msg with
      | RequestLicense(reply) when Set.count usedKeys < licenses.Length ->
        let key,license =
          licenses
          |> Seq.filter (fst >> usedKeys.Contains >> not)
          |> Seq.head
        reply.Reply (Some license)
        return! loop (usedKeys.Add key)
      | RequestLicense(reply) ->
        reply.Reply None
        return! loop usedKeys
      | ReleaseLicense(key, reply) ->
        let newSet = usedKeys.Remove(key)
        if newSet.Count < usedKeys.Count then
          reply.Reply true
        else
          reply.Reply false
        return! loop newSet
      | GetLicenseUsage(reply) ->
        reply.Reply(usedKeys.Count, licenses.Length)
        return! loop usedKeys
    }
    loop Set.empty
  )
  
  let serverConfig =
    { defaultConfig with
        bindings = [ HttpBinding.mkSimple HTTP "0.0.0.0" port
                     // TODO insert HTTPS binding here
                   ]
    }

  let app : WebPart =
    choose [
      GET >=> choose [
        path "/" >=> OK "Hello"
        path "/GetLicense" >=> (fun ctx -> async {
          let! license = licenseManager.PostAndAsyncReply RequestLicense
          match license with
          | Some(license) -> 
            let sb = System.Text.StringBuilder()
            use signer = new ECDsaCng(key)
            let signature = signer.SignData(Encoding.UTF8.GetBytes(license))
            let response = 
              sb.AppendLine(license)
                .Append(Convert.ToBase64String(signature))
                .ToString()
            printfn "Served license"
            return! ctx |> OK response
          | None ->
            printfn "All licenses used."
            return! ctx |> RequestErrors.FORBIDDEN "No free license."
        })
        pathScan "/ReleaseLicense/%s" (fun token ->
          fun ctx -> async {
            let! result = licenseManager.PostAndAsyncReply (fun reply -> ReleaseLicense(token, reply))
            if result then printfn "Got a license back."
                           return! ctx |> OK "Released."
            else           return! ctx |> OK "Unknown token."
          }
        )
        path "/Licenses" >=> (fun ctx -> async {
          let! used,total = licenseManager.PostAndAsyncReply GetLicenseUsage
          return! ctx |> (OK (sprintf "%d of %d licenses used." used total))
        })
      ]
    ]

  let publicKey = key.Export(CngKeyBlobFormat.EccPublicBlob) |> Convert.ToBase64String
  printfn "Server id:"
  printfn "%s" publicKey
  printfn ""
  Clipboard.SetText(publicKey)
  printfn "Copied server id to clipboard."
  startWebServer serverConfig app
  
  0
