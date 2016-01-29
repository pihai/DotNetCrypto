#r "System.Security"
#load "Common.fsx"

open Common
open System.Security.Cryptography

let cipher = 
  ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser)
let decryptedBytes = 
  ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser)
let decryptedTest = decode decryptedBytes

// The optional entropy is comparable with an initialization vector to add some randomness
// one of the problems of DPAPI is that i relies on the stength of the users password