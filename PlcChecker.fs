
module PlcChecker

open Absyn
open Environ

// The type checker can be seen as an interpreter that computes
// the type of an expression instead of its value.

let rec findMatch (v : expr) (elist: (expr option * expr) list) : expr =
  match v, elist with
  | v1, [] -> failwith "Match: no match found"
  | v1, (Some (eo), e1) :: t -> if (v1 = eo) then e1 else (findMatch v t)
  | v1, (None, e1) :: t -> e1

let rec teval (e : expr) (env : plcType env) : plcType =
    match e with
    | List [] -> ListT []
    | List es -> ListT (List.map (fun e -> teval e env) es)
    | ConI i -> IntT
    | ConB b -> BoolT
    | Var x -> lookup env x
    | ESeq s -> SeqT s                                             // might be more to this one

    | Let(x, e1, letBody) -> let xTyp = teval e1 env 
                             let letBodyEnv = (x, xTyp) :: env
                             teval letBody letBodyEnv  

    | Prim1 (op, e1) -> let i1 = teval e1 env in
                          match op with
                          | "hd" -> failwith "implement me"
                          | "tl" -> failwith "implement me"
                          | "ise" -> failwith "implement me"
                          | "-" -> if (i1 = IntT) then (IntT; IntT) else failwith ("Prim1: cannot use negation on type " + (type2string i1))
                          | "!" -> if (i1 = BoolT) then (BoolT; BoolT) else failwith ("Prim1: cannot use not on type " + (type2string i1))
                          | _ -> failwith ("Prim1: undefined unary operator " + op)
    
    | Prim2 (op, e1, e2) -> let i1 = teval e1 env in
                              let i2 = teval e2 env in
                                match op with
                                | ";" -> failwith "implement me"
                                | "::" -> failwith "implement me"
                                | "&&" -> if (i1 = BoolT && i1 = i2) then (BoolT; BoolT; BoolT) else failwith ("Prim2: cannot AND two non-int types " + (type2string i1) + " and " + (type2string i2))
                                | "<=" -> if (i1 = IntT && i1 = i2) then (IntT; IntT; BoolT) else failwith ("Prim2: cannot LTE compare non-int types " + (type2string i1) + " and " + (type2string i2))
                                | "/" -> if (i1 = IntT && i1 = i2) then (IntT; IntT; IntT) else failwith ("Prim2: cannot div non-int types " + (type2string i1) + " and " + (type2string i2))
                                | "*" -> if (i1 = IntT && i1 = i2) then (IntT; IntT; IntT) else failwith ("Prim2: cannot multiply non-int types " + (type2string i1) + " and " + (type2string i2))
                                | "+" -> if (i1 = IntT && i1 = i2) then (IntT; IntT; IntT) else failwith ("Prim2: cannot add non-int types " + (type2string i1) + " and " + (type2string i2))
                                | "-" -> if (i1 = IntT && i1 = i2) then (IntT; IntT; IntT) else failwith ("Prim2: cannot subtract non-int types " + (type2string i1) + " and " + (type2string i2))
                                | "!=" -> if (i1 = i2) then (i1; i2; BoolT) else failwith ("Prim2: mismatching equivalence return types " + (type2string i1) + " and " + (type2string i2))
                                | "=" -> if (i1 = i2) then (i1; i2; BoolT) else failwith ("Prim2: mismatching equivalence return types " + (type2string i1) + " and " + (type2string i2))
                                | "<" -> if (i1 = IntT && i1 = i2) then (IntT; IntT; BoolT) else failwith ("Prim2: cannot LT compare non-int types " + (type2string i1) + " and " + (type2string i2))
                                | _   -> failwith ("Prim2: undefined binary operator " + op)
    
    | Anon (t, s , e1) -> failwith "implement me"
    
    | Match (e1, elist) -> teval (findMatch e1 elist) env
    
    | Letrec(f, x, xTyp, fBody, rTyp, letBody) -> failwith "need to update old Letfun implementation below"
                                                  (*
                                                  let fTyp = FunT(xTyp, rTyp) 
                                                  let fBodyEnv = (x, xTyp) :: (f, fTyp) :: env
                                                  let letBodyEnv = (f, fTyp) :: env
                                                  if teval fBody fBodyEnv = rTyp
                                                    then teval letBody letBodyEnv
                                                  else failwith ("Letfun: return type in " + f + " of " + type2string rTyp + " does not match " + type2string (teval fBody fBodyEnv))   
                                                  *)

    | If (e1, e2, e3) -> match (teval e1 env), (teval e2 env), (teval e3 env) with
                         | BoolT, t2, t3 when (t2 = t3) -> t2
                         | BoolT, t2, t3 when (t2 <> t3) -> failwith ("If: different return types " + (type2string t2) + ", " + (type2string t3))
                         | t1, t2, t3 -> failwith ("If: first expression not bool: " + (type2string t1))
    
    | Call(Var f, eArg) -> match lookup env f with
                           | FunT(xTyp, rTyp) -> if teval eArg env = xTyp 
                                                   then rTyp 
                                                 else failwith ("Call: wrong argument type " + type2string (teval eArg env))
                           | _ -> failwith ("Call: unknown function " + type2string (lookup env f))
    
    | Item (n, e1) -> match teval e1 env with
                      | ListT vs -> try 
                                      List.item (n - 1) vs
                                    with
                                      | :? System.ArgumentException -> failwith ("Item: index out of range with position " + string n)
                      | _ -> failwith ("Item: not a list type " + type2string (teval e1 env))
