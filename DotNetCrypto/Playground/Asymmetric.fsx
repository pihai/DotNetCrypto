#load "Common.fsx"

open Common
open System.Security.Cryptography
open System.IO
open System

let encrypt plain rsaParams =
  use rsa = new RSACryptoServiceProvider()
  rsa.ImportParameters rsaParams
  rsa.Encrypt(plain, true)

let decrypt cipher rsaParams =
  use rsa = new RSACryptoServiceProvider()
  rsa.ImportParameters rsaParams
  rsa.Decrypt(cipher, true)

let rsa = new RSACryptoServiceProvider()

// encrypt with the public key
let cipherBytes = encrypt plainBytes (rsa.ExportParameters(false))

// decrypt with the private key
let decryptedBytes = decrypt cipherBytes (rsa.ExportParameters(true))
let decryptedText = decode decryptedBytes


// How to use a key store
// Automatically creates a key container if it doesn't exit.
// Or loads the existing key if a container with such name exists
let cp = new CspParameters()
cp.KeyContainerName <- "MyKeyContainer2"
//cp.Flags <- CspProviderFlags.UseMachineKeyStore
(new RSACryptoServiceProvider(cp)).ToXmlString(true)

// Get the location of the key store
let containerInfo = new CspKeyContainerInfo(cp)
let containerPath =
  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
               @"Microsoft\Crypto\RSA\MachineKeys")
let containerName = containerInfo.UniqueKeyContainerName

Environment.CurrentDirectory
Environment.SpecialFolder.LocalApplicationData.ToString()