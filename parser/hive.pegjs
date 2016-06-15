{
	function token(val) { return val.join(""); }
    function isSeparator(char) { return /\s/.test(char); }
    function isForbidden(char) { return "[](){}\".'|:".includes(char); }
    
    function variable(token) {
    	return { type: 'VariableNode', value: token };
	}
    function literal(token) {
    	return { type: 'LiteralNode', value: token };
    }
    function message(selector, args) {
    	return {
            selector: selector,
            args: args
        };
    }
    function flatten(msg, tail) {
    	/*
        Returns a list of messages
        */
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
    
    function assignment(left, right) {
    	return {
        	type: 'Assignment',
            left: left,
            right: right
        };
    }
}

start = expression

digits = $[0-9]+
integer "integer" = token:$("-"? digits) { return parseInt(token); }
float "float" = token:$(integer "." digits) { return parseFloat(token); }
number "number" = (float / integer)

letter = [a-zA-Z]
word = [a-zA-Z0-9]
identifier "identifier" = $(letter word*)

keywordSelector "keyword selector" = $(identifier ":")
unarySelector "unary selector" 	= identifier
binarySelector "binary selector" 
	= $(char:[^a-zA-Z0-9] & { return !isSeparator(char) && !isForbidden(char); })+

variable "variable" = token:identifier { return variable(token); }
reference "reference" = variable
literal "literal" = token:(number / string) { return literal(token); }

expression = assignment / keywordSend / binarySend
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

comments "comments" = $(["][^"]*["])+
separator "separator" = (char:. & { return isSeparator(char); })+
ws = (separator / comments)*

string = ['] val:(("''" {return "'"} / [^'])*) ['] { return token(val); }
