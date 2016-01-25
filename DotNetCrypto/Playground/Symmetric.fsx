#load "Common.fsx"

open Common
open System.IO
open System.Security.Cryptography

let encrypt plain =
  use aes = Aes.Create()
  // shortcut for: use aes = new AesCryptoServiceProvider()

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

let cipher, key, iv = encrypt plainBytes
let decrypted = decrypt cipher key iv
