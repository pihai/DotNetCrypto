#load "Common.fsx"

open Common
open System.IO
open System.Security.Cryptography

module HMAC =
  // Secure Hash-Function with a key (MAC = Message Authentication Code)
  // This algorithm is pretty fast, but both sides need to know the key

  // Automatically creates a 64 byte long key
  // Normally the key would be loaded from a secure store and set manually
  let hashAlgo1 = HMACSHA256.Create()
  let hash1 = hashAlgo1.ComputeHash(plainBytes)

  let hashAlgo2 = HMACSHA256.Create()
  hashAlgo2.Key <- hashAlgo1.Key
  let hash2 = hashAlgo2.ComputeHash(plainBytes)

  hash1 = hash2

module RSA =
  let hashAlgo = new SHA256Managed()

  let rsa = RSA.Create()
  let privateKey = rsa.ExportParameters true
  let publicKey = rsa.ExportParameters false

  let rsaPriv = new RSACryptoServiceProvider()
  rsaPriv.ImportParameters privateKey
  let signature = rsaPriv.SignData(plainBytes, hashAlgo)

  let rsaPub = new RSACryptoServiceProvider()
  rsaPub.ImportParameters publicKey
  rsaPub.VerifyData(plainBytes, hashAlgo, signature)

  let cp = new CspParameters()
  cp.KeyContainerName <- "foo"

  let foo = new RSACryptoServiceProvider(cp)

  printfn "%A" (foo.ToXmlString(true))

  let cp2 = new CspParameters()
  cp2.KeyContainerName <- "foo"

  let bar = new RSACryptoServiceProvider(cp2)
  printfn "%A" (bar.ToXmlString(true))
