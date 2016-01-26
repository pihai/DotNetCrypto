open System.Security.Cryptography

let random = RandomNumberGenerator.Create()
let array = Array.zeroCreate<byte> 20
random.GetBytes(array)