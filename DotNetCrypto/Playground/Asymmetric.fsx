#load "Common.fsx"

open Common
open System.Security.Cryptography
open System.IO

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
