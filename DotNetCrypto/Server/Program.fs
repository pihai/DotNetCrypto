// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

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

let port = 8083

let privateKeyBytes = System.Convert.FromBase64String("RUNTMiAAAACvCjDrBMt8pZGjdy4OpXfj/KEhnzFvRK7097otjloCOoJGCA3upVQBuWB8TAgU5FcY0uSFE8MEmK2HyKrOvrrd04MhHt81twN0v0vxpaZQ2idSIVmo1/lG+ICN6kk2H44=")

type private Commands = 
  | RequestLicense of AsyncReplyChannel<string option>
  | ReleaseLicense of string * AsyncReplyChannel<bool>
  | GetLicenseUsage of AsyncReplyChannel<int * int>

let loadLicensesFromXML () =
  let doc = XmlDocument()
  doc.Load @"LicenseConfig.xml"
  doc.SelectSingleNode("//nrOfLicenses").InnerXml |> System.Int32.Parse

[<EntryPoint>]
let main argv =
  let licenseManager = MailboxProcessor.Start(fun inbox ->
    let generateToken () = System.Guid.NewGuid().ToString()
    let rec loop (freeLicenseCount, usedTokens) = async {
      let! msg = inbox.Receive()

      match msg with
      | RequestLicense(reply) when freeLicenseCount > 0 ->
        let token = generateToken()
        reply.Reply (Some token)
        return! loop (freeLicenseCount - 1, usedTokens |> Set.add token )
      | RequestLicense(reply) ->
        reply.Reply None
        return! loop (freeLicenseCount,usedTokens)
      | ReleaseLicense(token, reply) ->
        let newSet = usedTokens |> Set.remove token
        if newSet.Count < usedTokens.Count then
          reply.Reply true
          return! loop (freeLicenseCount + 1, newSet)
        else
          reply.Reply false
          return! loop (freeLicenseCount, usedTokens)
      | GetLicenseUsage(reply) ->
        reply.Reply(usedTokens.Count, usedTokens.Count + freeLicenseCount)
        return! loop (freeLicenseCount, usedTokens)
    }
    let availableLicenses = loadLicensesFromXML()
    loop (availableLicenses, Set.empty)
  )
  
  let serverConfig =
    { defaultConfig with
        bindings = [ HttpBinding.mkSimple HTTP "0.0.0.0" port
                     // TODO insert HTTPS binding here
                   ]
    }

  ///  we can still use the old symbol but now has a new meaning
  let foo : WebPart = fun ctx -> GET ctx >>= OK "hello"

  let app : WebPart =
    choose [
      GET >=> choose [
        path "/" >=> OK "Hello"
        path "/GetLicense" >=> (fun ctx -> async {
          let! license = licenseManager.PostAndAsyncReply RequestLicense
          if license.IsSome then
            let sb = System.Text.StringBuilder()
            use signer = new ECDsaCng(CngKey.Import(privateKeyBytes, CngKeyBlobFormat.EccPrivateBlob))
            let signature = signer.SignData(Encoding.UTF8.GetBytes(license.Value))

            let response = 
              sb.AppendLine(license.Value)
                .Append(Convert.ToBase64String(signature))
                .ToString()

            return! ctx |> OK response
          else
            return! ctx |> RequestErrors.FORBIDDEN "No free license."
        })
        pathScan "/ReleaseLicense/%s" (fun token ->
          fun ctx -> async {
            let! result = licenseManager.PostAndAsyncReply (fun reply -> ReleaseLicense(token, reply))
            if result then
              return! ctx |> OK "Released."
            else
              return! ctx |> OK "Unknown token."
          }
        )
        path "/Licenses" >=> (fun ctx -> async {
          let! used,total = licenseManager.PostAndAsyncReply GetLicenseUsage
          return! ctx |> (OK (sprintf "%d of %d licenses used." used total))
        })
      ]
    ]

  startWebServer serverConfig app

//  startWebServer serverConfig app

  printfn "%A" argv
  0 // return an integer exit code
