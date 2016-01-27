open System
open System.IO
open System.Text
open System.Security.Cryptography

let privateKeyBytes = Convert.FromBase64String("RUNTMiAAAACvCjDrBMt8pZGjdy4OpXfj/KEhnzFvRK7097otjloCOoJGCA3upVQBuWB8TAgU5FcY0uSFE8MEmK2HyKrOvrrd04MhHt81twN0v0vxpaZQ2idSIVmo1/lG+ICN6kk2H44=")

let generateKey () =
  let key = Array.zeroCreate<byte> 20
  RandomNumberGenerator.Create().GetNonZeroBytes(key)
  Convert.ToBase64String(key)

let generateLicensesXml publicKey nrOfLicenses =
  use signer = new ECDsaCng(CngKey.Import(privateKeyBytes, CngKeyBlobFormat.EccPrivateBlob))
  List.init nrOfLicenses (fun _ ->
    let keyData = sprintf "<nr>%s</nr><pub-key>%s</pub-key>" (generateKey()) publicKey
    let signature = signer.SignData(Encoding.UTF8.GetBytes(keyData)) |> Convert.ToBase64String
    sprintf "<license><data>%s</data><sig>%s</sig></license>" keyData signature)

[<EntryPoint>]
let main argv = 
  printfn "Enter the number of licenses:"
  let nrOfLicenses = Console.ReadLine() |> Int32.Parse
  printfn "Please enter the public key of the license server:"
  let publicKey = Console.ReadLine()

  let keyFile = generateLicensesXml

  File.WriteAllLines("keys.txt", generateLicensesXml publicKey nrOfLicenses)

  0
