{
    function isSeparator(char) { return /\s/.test(char); }
    function isForbidden(char) { return "[](){}\".'|:".includes(char); }
    
    function variable(token) {
    	return { type: 'Variable', value: token };
	}
    function number(value) {
    	return { type: 'Number', value: value };
    }
    function string(value) {
    	return { type: 'String', value: value.join("") };
    }
    function assignment(left, right) {
    	return {
        	type: 'Assignment',
            left: left,
            right: right
        };
    }
    function array(first, rest) {
    	rest.unshift(first);
        return {
        	type: 'Array',
        	elements: rest
        };
    }
    function message(selector, args) {
    	return {
            selector: selector,
            args: args
        };
    }
    function flatten(msg, tail) {
    	if (tail == undefined) {
        	return [msg];
        } else {
    		tail.unshift(msg);
            return tail;
        }
    }
    function send(rcvr, msgs) {
    	if (msgs === null) {
        	return rcvr;
        } else {
            var result = null;
            msgs.forEach(function (msg) {
                result = {
                    type: 'MessageSend',
                    rcvr: result === null ? rcvr : result,
                    selector: msg.selector,
                    args: msg.args
                };
            });
            return result;
        }
    }
    
    function unaryMessage(selector) { return message(selector, []); }
    function unaryTail(msg, tail) { return flatten(msg, tail); }
    function unarySend(rcvr, msgs) { return send(rcvr, msgs); }
    
    function binaryMessage(selector, arg) { return message(selector, [arg]); }
    function binaryTail(msg, tail) { return flatten(msg, tail); }
    function binarySend(rcvr, msgs) { return send(rcvr, msgs); }
    
    function keywordPair(key, arg) { return { key: key, arg: arg }; }
    function keywordMessage(pairs) {
    	var selector = pairs.map(function (pair) { return pair.key; }).join("");
        var args = pairs.map(function (pair) { return pair.arg; });
    	return message(selector, args);
    }
    function keywordSend(rcvr, msg) { return send(rcvr, [msg]); }
    
    function methodBody(first, rest) {
    	rest.unshift(first);
        return rest.filter(function (e) { return e; });
    }
    function method(decl, temps, body) {
    	if (!decl) { decl = { selector: "", args: [] }; }
        if (!temps) { temps = []; }
        return {
        	type: 'Method',
            selector: decl.selector,
            args: decl.args,
            temps: temps,
        	body: body
        };
    }
    
    function jsStatement(src) {
    	return {
        	type: "Javascript",
            code: src
        };
    }
}

start = methodBody

digits = $[0-9]+
integer "integer" = token:$("-"? digits) { return parseInt(token); }
float "float" = token:$(integer "." digits) { return parseFloat(token); }
number "number" = val:(float / integer) { return number(val); }
string = ['] val:(("''" {return "'"} / [^'])*) ['] { return string(val); }
array = '{' ws first:expression? rest:('.' ws expr:expression { return expr; })* ws '}' { return array(first, rest); }
literal "literal" = (number / string / array / method)

letter = [a-zA-Z]
word = [a-zA-Z0-9]
identifier "identifier" = $(letter word*)

keywordSelector "keyword selector" = $(identifier ":")
unarySelector "unary selector" 	= identifier
binarySelector "binary selector" 
	= $(char:[^a-zA-Z0-9] & { return !isSeparator(char) && !isForbidden(char); })+

variable "variable" = token:identifier { return variable(token); }
reference "reference" = variable

expression = assignment / keywordSend / binarySend / jsStatement
subexpression  = '(' ws expression:expression ws ')' { return expression; }
operand = literal / reference / subexpression

unaryMessage = ws selector:unarySelector ![:] { return unaryMessage(selector); }
unaryTail = msg:unaryMessage ws tail:unaryTail? ws { return unaryTail(msg, tail); }
unarySend = rcvr:operand ws msg:unaryTail? { return unarySend(rcvr, msg); }

binaryMessage = ws selector:binarySelector ws arg:(unarySend / operand) {
	return binaryMessage(selector, arg);
}
binaryTail = msg:binaryMessage ws tail:binaryTail? { return binaryTail(msg, tail); }
binarySend = rcvr:unarySend msg:binaryTail? { return binarySend(rcvr, msg); }

keywordPair = key:keywordSelector ws arg:binarySend ws { return keywordPair(key, arg); }
keywordMessage = ws pairs:(pair:keywordPair ws { return pair; })+ { return keywordMessage(pairs); }
keywordSend = rcvr:binarySend msg:keywordMessage { return keywordSend(rcvr, msg); }

message = binaryMessage / unaryMessage / keywordMessage

assignment = left:variable ws ":=" ws right:expression { return assignment(left, right); }

methodDeclaration = decl:(anonMethod / keywordMethod / unaryMethod / binaryMethod) ws '|' { return decl; }
anonMethod = vars:(':' ws varName:identifier ws { return varName; })+ { 
	return { selector: "", args: vars }
}
keywordMethod = kvps:(key:keywordSelector ws arg:identifier ws { return {key: key, arg: arg}; })+ { 
	return keywordMessage(kvps); 
}
unaryMethod = sel:identifier { return unaryMessage(sel); }
binaryMethod = sel:binarySelector ws arg:identifier { return binaryMessage(sel, arg); }

methodBody = ws first:expression? rest:('.' ws expr:expression { return expr; })* ws '.'? { return methodBody(first, rest); }
method = '[' ws decl:methodDeclaration? ws temps:temps? ws body:methodBody ws ']' { return method(decl, temps, body); }

jsStatement "Javascript statement" 
	= "<" val:((">>" {return ">"} / [^>])*) ">" { return jsStatement(val.join("")); }

temps
	= "|" vars:(ws variable:identifier ws { return variable; })* "|" { return vars; }

comments "comments" = $(["][^"]*["])+
separator "separator" = (char:. & { return isSeparator(char); })+
ws = (separator / comments)*
