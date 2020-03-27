module APIPropertyBasedTesting.Program
open System
open FsCheck
open FSharp.Data

type cmd =
    | GET of (string * int)
    | POST of string
    | PATCH of (string * int)
    | DELETE of (string * int)


[<EntryPoint>]
let main argv =
    let httpArbList =
        Arb.fromGen
            (Gen.listOf
                (Gen.oneof
                    [ gen {
                        let! i = Gen.choose (0, 500000)
                        return cmd.GET("http://localhost:3000/api/shop/get/", i) }
                      gen { return cmd.POST("http://localhost:3000/api/shop/create/item") }
                      gen {
                          let! i = Gen.choose (0, 500000)
                          return cmd.PATCH("http://localhost:3000/api/shop/update/", i) }
                      gen {
                          let! i = Gen.choose (0, 500000)
                          return cmd.DELETE("http://localhost:3000/api/shop/delete/", i) } ]))



    let httpArb =
        Arb.fromGen
            (Gen.oneof
                [ gen {
                    let! i = Gen.choose (0, 5000000)
                    return ("http://localhost:3000/api/shop/get/", "GET", i) }
                  gen { return ("http://localhost:3000/api/shop/create/item", "POST", -1) }
                  gen {
                      let! i = Gen.choose (0, 5000000)
                      return ("http://localhost:3000/api/shop/update/", "PATCH", i) }
                  gen {
                      let! i = Gen.choose (0, 5000000)
                      return ("http://localhost:3000/api/shop/delete/", "DELETE", i) } ])


    let httpRespondCorrect xs =
        match xs with
        | (a, "POST", -1) -> Http.RequestString(a, httpMethod = "POST") = "POST"
        | (a, "GET", c) -> Http.RequestString(a + string c, httpMethod = "GET") = "GET " + string c
        | (a, "PATCH", c) -> Http.RequestString(a + string c, httpMethod = "PATCH") = "PATCH " + string c
        | (a, "DELETE", c) -> Http.RequestString(a + string c, httpMethod = "DELETE") = "DELETE " + string c

    let httptest x = Prop.forAll httpArb (httpRespondCorrect)
    Check.Quick httptest

    let httpRespondCorrectTwo httpReq =
        match httpReq with
        | GET (url,id) -> Http.RequestString(url + string id, httpMethod = "GET") = "GET " + string id
        | POST (url) -> Http.RequestString(url, httpMethod = "POST") = "POST"
        | PATCH (url,id) -> Http.RequestString(url + string id, httpMethod = "PATCH") = "PATCH " + string id
        | DELETE (url, id) -> Http.RequestString(url + string id, httpMethod = "DELETE") = "DELETE " + string id
           
            
    
    let rec httpListRespondCorrect list =
        match list with
        | [] -> true
        | head :: tail -> httpRespondCorrectTwo head && httpListRespondCorrect tail
        
        
    let httpListTest x = Prop.forAll httpArbList (httpListRespondCorrect)
    Check.Quick httpListTest

    0 // return an integer exit code
