module APIPropertyBasedTesting.ApiTest

type API() =
    member __.Get(id) = 
    member __.Post() = 
    member __.Patch(id) = 
    member __.Delete(id) = 
    member __.ToString() = sprintf "API=%i"