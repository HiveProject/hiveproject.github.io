
var compiler = (function (parser) {
	
	function compile(string) {
		var ast = parser.parse(string);
		var js = ast.map(visitStatement).join("\n");
		return js;
	}
	
	function visitStatement(statement) {
		// Statements should finish with a semicolon
		return visit(statement) + ";";
	}
	
	function visit(expr) {
		// TODO(Richo): Maybe replace with an actual visitor...
		switch (expr.type) {
			case "Variable": 
				return visitVariable(expr); 
				break;
			case "Number": 
				return visitNumber(expr); 
				break;
			case "String": 
				return visitString(expr); 
				break;
			case "Assignment": 
				return visitAssignment(expr); 
				break;
			case "Array": 
				return visitArray(expr); 
				break;
			case "MessageSend": 
				return visitMessageSend(expr); 
				break;
			case "Method": 
				return visitMethod(expr); 
				break;
			case "Javascript": 
				return visitJavascript(expr); 
				break;
		}
	}
	
	function visitVariable(expr) {
		return "context.hiveLookup('" + expr.value + "').get()";
	}
	
	function visitNumber(expr) {
		return expr.value;
	}
	
	function visitString(expr) {
		return "'" + expr.value + "'";
	}
	
	function visitAssignment(expr) {
			return "context.hiveLookup('" + expr.left.value + "').set("+visit(expr.right)+"); context.hiveLookup('" + expr.left.value + "').get();"; 
	}
	
	function visitArray(expr) {
		return "[" + expr.elements.map(visit).join(", ") + "]";
	}
	
	function visitMessageSend(expr) {
		return visit(expr.rcvr) + ".receive('" + 
			expr.selector + "')(" +
			expr.args.map(visit).join(", ") + ")";
	}
	
	function visitMethod(expr) {
		return `CreateMethod("` +  expr.selector+
		`","`+  "("+"function " + "(" +
			expr.args.join(", ") + ") {" +
			expr.args.map(function(item){return "context.set('"+item+"',"+item+");";}).join("")+
			expr.temps.map(function (tmp) { 
				return "var " + tmp + ";";
			}).join(" ") +
			"return (" +expr.body.map(visit).join("; ").replace(/"/g,'\\"') + `); })",context)`
	}
	
	function visitJavascript(expr) {
		return expr.code;
	}
	
	function evaluate(string) {
		return HiveEval(model.getRoot().get("context"), compiler.compile(string));
	}
	
	Object.prototype.receive = function (selector) {
		var method = this.hiveLookup(selector).get();
		if (method != null) {
			var currentExecutionContext=CreateContext(method.get("context"));
		 	currentExecutionContext.set('self',this);
			return HiveEval(currentExecutionContext, method.get("source"));
		}
		return function () { return "DNU"; }
	}
	
	Object.prototype.hiveLookup = function(selector,context){
			if(context.keys().includes(selector))
			{
				return {
					get:function(){return context.get(selector);},
					set:function(value){context.set(selector,value);},
					found:true
					};
			}
			if(context.keys().includes('parent'))
			{
			var parentSlot= context.get('parent').hiveLookup(selector);
				if(parentSlot.found)
				{return parentSlot;}
			}  
			return {
					get:function(){return context.get(selector);},
					set:function(value){context.set(selector,value);},
					found:false
					
					};
		}
	
	return {
		compile: compile,
		evaluate: evaluate
	};
})(parser);