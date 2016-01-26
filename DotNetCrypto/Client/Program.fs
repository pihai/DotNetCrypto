// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System.Net.Http
open System.Net

let port = 8083

let requestLicenseToken ipAddress = async {
  use client = new HttpClient()
  let! response = client.GetAsync(sprintf "http://%s:%d/GetLicense" ipAddress port) |> Async.AwaitTask
  if response.IsSuccessStatusCode then
    let! token = response.Content.ReadAsStringAsync() |> Async.AwaitTask
    if not <| System.String.IsNullOrEmpty(token) then
      return Some token
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
