#load "Common.fsx"

open Common
open System.IO
open System.Security.Cryptography

// Automatically creates a 64 byte long key
// Normally the key would be loaded from a secure store and set manually
let hashAlgo1 = HMACSHA256.Create()
let hash1 = hashAlgo1.ComputeHash(plainBytes)

let hashAlgo2 = HMACSHA256.Create()
hashAlgo2.Key <- hashAlgo1.Key
let hash2 = hashAlgo2.ComputeHash(plainBytes)

hash1 = hash2