module Common

open System.Text

let encode (text: string) = Encoding.UTF8.GetBytes text
let decode (bytes: byte array) = Encoding.UTF8.GetString bytes

let plainText = "This is a highly confidential message"
let plainBytes = encode plainText