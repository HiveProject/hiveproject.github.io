
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
		return "context.get('" + expr.value + "')";
	}
	
	function visitNumber(expr) {
		return expr.value;
	}
	
	function visitString(expr) {
		return "'" + expr.value + "'";
	}
	
	function visitAssignment(expr) {
		return visit(expr.left) + " = " + visit(expr.right);
	}
	
	function visitArray(expr) {
		return "[" + expr.elements.map(visit).join(", ") + "]";
	}
	
	function visitMessageSend(expr) {
		return visit(expr.rcvr) + ".get('" + 
			expr.selector + "')(" +
			expr.args.map(visit).join(", ") + ")";
	}
	
	function visitMethod(expr) {
		return "(function " + expr.selector.replace(/:/g, "_") + "(" +
			expr.args.join(", ") + ") {" +
			expr.temps.map(function (tmp) { 
				return "var " + tmp + ";";
			}).join(" ") +
			expr.body.map(visit).join("; ")	+ "})"
	}
	
	function visitJavascript(expr) {
		return expr.code;
	}
	
	function evaluate(string) {
		return compiler.compile(string);
	}
	
	return {
		compile: compile,
		evaluate: evaluate
	};
})(parser);