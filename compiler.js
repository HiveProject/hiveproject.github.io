
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
		return "context.lookup('" + expr.value + "').get()";
	}
	
	function visitNumber(expr) {
		return "("+expr.value+")";
	}
	
	function visitString(expr) {
		return "CreateString('" + expr.value + "')";
	}
	
	function visitAssignment(expr) {
			return "context.lookup('" + expr.left.value + "').set("+visit(expr.right)+")"; 
	}
	
	function visitArray(expr) {
		return "model.createList([" + expr.elements.map(visit).join(", ") + "])";
	}
	
	function visitMessageSend(expr) {
		return visit(expr.rcvr) + ".receive('" + 
			expr.selector + "')(" +
			expr.args.map(visit).join(", ") + ")";
	}
	
	function visitMethod(expr) {
		if (expr.body.type && expr.body.type === "Javascript") {
			return 'CreateMethod("' + expr.selector + '", "(function (' +
				expr.args.join(", ") + ") {" +
				expr.args.map(function(item){return "context.set('"+item+"',"+item+");";}).join("")+
				expr.body.code.replace(/\\/g,'\\\\').replace(/"/g,'\\"') + '})", context)';
		} else {			
			var body = expr.body.map(visit);
			body.push("return ("+ body.pop()+")");
			return `CreateMethod("` +  expr.selector+
			`","`+  "("+"function " + "(" +
				expr.args.join(", ") + ") {" +
				expr.args.map(function(item){return "context.set('"+item+"',"+item+");";}).join("")+
				expr.temps.map(function (tmp) { 
					return 'context.set("' + tmp + '", context.lookup("null").get());';
				}).join(" ") +
				body.join("; ").replace(/\\/g,'\\\\').replace(/"/g,'\\"') + `; })",context)`;
		}
	}
	
	function visitJavascript(expr) {
		return expr.code;
	}
	
	function evaluate(string) {
		return HiveEval(model.getRoot().get("context"), compiler.compile(string));
	}
	
	Object.prototype.receive = function (selector) {
		var method = this.lookup(selector).get();
		if (method != null) {
			var currentExecutionContext=CreateContext(method.get("context"));
		 	//currentExecutionContext.set('self',this);
			return HiveEval(currentExecutionContext, method.get("source"));
		}
		return function () { return CreateString("DNU: " + selector); }
	}
	  
	Object.prototype.lookup = function(selector){
			var me = this;
			var myType=me.type;
			switch(myType) {
				case "List":
					//Array
					return staticLookup("List",me,selector);
				break;
				case "EditableString":
					//String
					return staticLookup("String",me,selector);
				break;
			}
			if(this.keys().includes(selector))
			{
				return {
					get:function(){return me.get(selector);},
					set:function(value){me.set(selector,value);return value;},
					found:true
					};
			}
			if(this.keys().includes('parent'))
			{
			var parentSlot= this.get('parent').lookup(selector);
				if(parentSlot.found)
				{return parentSlot;}
			}  
			return {
					get:function(){return me.get(selector);},
					set:function(value){me.set(selector,value);return value;},
					found:false
					
					};
		};
	var staticLookup = function(parnetName,selfValue,selector){
		var parent=model.getRoot().get('context').lookup(parnetName).get(); 
		if(parent!=null) {
			var real= parent.lookup(selector).get();
			var method= model.createMap();
			if(real==null)
			{return {
						get:function(){return parent.get(selector);},
						set:function(value){parent.set(selector,value);return value;},
						found:false
						
						};}
			var ct = CreateContext(real.get('context'));
				ct.set('self',selfValue);
				method.set('context',ct);
				method.set('source',real.get('source'));
				method.set("selector",selector);
				method.set("value",method);
			return {
						get:function(){return method;},
						set:function(value){parent.set(value);return value;},
						found:true
						};
			}else{return CreateString('DNU: ' + selector)}};
			
	Number.prototype.lookup = function(selector){ return staticLookup('Number',this.valueOf(),selector);}; 
			
	return {
		compile: compile,
		evaluate: evaluate
	};
})(parser);