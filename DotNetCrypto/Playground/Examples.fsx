//#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6\System.Security.dll"

open System.Security.Cryptography
open System.Text
open System.IO
open System

let data = Encoding.UTF8.GetBytes("Hello world.")

let Encryption_Aes =
  // (secret = 128)	(top secret = 256 [default])
  let encrypt plain =
    use aes = Aes.Create() // shortcut for: use aes = new AesCryptoServiceProvider()
    use ms = new MemoryStream()
    use cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write)
    cs.Write(plain, 0, Array.length plain)
    cs.FlushFinalBlock()
    ms.ToArray(), aes.Key, aes.IV

  let decrypt cipher key iv =
    use aes = Aes.Create()
    use ms = new MemoryStream(cipher, false)
    use cs = new CryptoStream(ms, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read)
    use br = new BinaryReader(cs)
    use sr = new StreamReader(cs)
    sr.ReadToEnd()

  let cipher, key, iv = encrypt data
  let decrypted = decrypt cipher key iv
  ()

let DigitalSignature_ECDSA =
  // (secret = 256)	(top secret = 384)
  use algo = new ECDsaCng() // 521 default
  let signature = algo.SignData(data)
  let valid = algo.VerifyData(data, signature)
  let tampered = Encoding.UTF8.GetBytes("Hello me.")
  let invalid = algo.VerifyData(tampered, signature)
  ()

let KeyExchange_ECDiffieHellman =
  use alice = new ECDiffieHellmanCng()
  use bob = new ECDiffieHellmanCng()

  let bobKey = bob.DeriveKeyMaterial(alice.PublicKey)
  let aliceKey = alice.DeriveKeyMaterial(bob.PublicKey)

  use malice = new ECDiffieHellmanCng()
  let maliceKey = malice.DeriveKeyMaterial(alice.PublicKey)
  ()

let Hashing =
  // (secret = 256)	(top secret = 384)
  use hashAlgo = SHA384Cng.Create()
  let hash = hashAlgo.ComputeHash(data)
  let hashLength = hash.Length // 384 / 8
  ()


    

