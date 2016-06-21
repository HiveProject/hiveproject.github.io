
var compiler = (function (parser) {
	
	function compile(string) {
		var ast = parser.parse(string);
		for (var i = 0; i < ast.length; i++) {
			var expr = ast[i];
			console.log(expr.type);
		}
	}
	
	return {
		compile: compile
	};
})(parser);