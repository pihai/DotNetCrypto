#r "System.Security"
#load "Common.fsx"

open Common
open System.Security.Cryptography

// The optional entropy is comparable with an initialization vector to add some randomness
let cipher = ProtectedData.Protect(plainBytes, Array.empty<byte>, DataProtectionScope.CurrentUser)
let decryptedBytes = ProtectedData.Unprotect(cipher, Array.empty<byte>, DataProtectionScope.CurrentUser)
let decryptedTest = decode decryptedBytes

// one of the problems of DPAPI is that i relies on the stength of the users password