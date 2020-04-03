module Program
open System
open System.Threading
open System.Linq
open System.Collections.Generic
open FsCheck
open FsCheck.Experimental
open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open Test

 
type ApiWrapper() =
    let url = "http://localhost:3000"
    member __.GetItem(id) = Thread.Sleep(10)
                            Http.RequestString(url + "/api/shop/get/" + id, httpMethod="GET")
    member __.CreateItem() = Thread.Sleep(10)
                             Http.RequestString(url + "/api/shop/create/item", httpMethod = "POST",
                                                    headers = [ ContentType HttpContentTypes.Json],
                                                    body = TextRequest """ {"test": 42} """)
    
    member __.Reset() = Http.RequestString(url + "/api/shop/reset", httpMethod = "POST")

let jsonParse json = JsonValue.Parse(json)

let findId (json:string, key:string) = let jsonParsed = jsonParse json
                                       let res = JsonExtensions.Item(jsonParsed, key).AsString()
                                       res

type ApiModel() =
    let model = Dictionary<int,string>()

    member __.LookupIx(index) = model.[index]
    //{
    //  "test": 42
    //  "id": 735
    //}
    member __.GetUserId(index) = let json = __.LookupIx(index)
                                 findId (json, "id")
    member __.Get(index) = model.[index]
    member __.Create(index) = model.Add(index,""" {"test": 42} """)
    member __.CreateReal(index, item) = model.Remove(index) |> ignore
                                        model.Add(index,item)
    member __.Contains(index) = model.ContainsKey(index)
    
    member __.Reset() = model.Clear
    override __.ToString() = String.Join(" ; ", model.Select(fun x -> String.Join("=",x.Key,x.Value)))
    //override __.ToString() = "API Model"
    
    
let apiSpec =
    let GetCmd index = {
            new Operation<ApiWrapper, ApiModel>() with
            member __.Run m = m
            override __.Pre m = m.Contains(index)
            member __.Check(sut, m) = let sutItem = sut.GetItem(m.GetUserId(index))
                                      let mItem = m.Get(index)
                                      String.Compare(mItem, sutItem) = 0 |@ sprintf "Get: model = %s, actual = %O" mItem sutItem
            override __.ToString() = "Get " + string index
            }
    let CreateCmd index = {
            new Operation<ApiWrapper, ApiModel>() with
             
            member __.Run m =  m.Create(index)
                               m
            override __.Pre m = not (m.Contains(index))
            member __.Check(sut,m) = let realItem = sut.CreateItem()
                                     m.CreateReal(index, realItem) |@ "Create: model = true, actual = true"
            override __.ToString() = "Create " + string index
            }
    let create = {
            new Setup<ApiWrapper,ApiModel>() with
            override __.Actual() = let wrap = ApiWrapper()
                                   wrap.Reset() |> ignore
                                   wrap
            override __.Model() = ApiModel()
            }                    
    
    {
      new Machine<ApiWrapper,ApiModel>() with
      override __.Setup = create |> Gen.constant |> Arb.fromGen
      override __.Next m = Gen.elements [ CreateCmd (Gen.choose(1,50) |> Gen.sample 0 1 |> Seq.exactlyOne); GetCmd (Gen.choose(1,50) |> Gen.sample 0 1 |> Seq.exactlyOne)]
    }
    
 
[<EntryPoint>]
let main argv =
    Check.One ({ Config.Quick with MaxTest = 5000; QuietOnSuccess=true }, StateMachine.toProperty apiSpec)
    //let toPrint = test "test" "test"
    //printf "%s" toPrint
    0 // return an integer exit code
