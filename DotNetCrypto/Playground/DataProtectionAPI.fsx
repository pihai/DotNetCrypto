#r "System.Security"
#load "Common.fsx"

open System.Security.Cryptography
open Common

// The optional entropy is comparable with an initialization vector to add some randomness
let cipher = ProtectedData.Protect(plainBytes, Array.empty<byte>, DataProtectionScope.CurrentUser)
let decryptedBytes = ProtectedData.Unprotect(cipher, Array.empty<byte>, DataProtectionScope.CurrentUser)
let decryptedTest = decode decryptedBytes