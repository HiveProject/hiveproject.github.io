
var REPL = (function () {
	
	var repl_in;
	var repl_out;
	var ready = false;
	
	var evalList = [];
	var evalIndex = 0;
	
	function init(in_textArea, out_div) {
		repl_in = in_textArea;
		repl_out = out_div;
		
		// INFO(Richo): 
		// For some reason, the ENTER key must be handled with onkeypress but the
		// arrow keys must be handled with onkeyup...
		repl_in.onkeypress = function () {
			if (event.which === 10 && event.ctrlKey) {
				// ENTER
				REPL.evaluate();
			}
		};
		repl_in.onkeyup = function () {
			if (event.which === 38 && event.ctrlKey) {
				// UP
				evalIndex--;
				update();
			} else if (event.which === 40 && event.ctrlKey) {
				// DOWN
				evalIndex++;
				update();
			}
		};
	}
	
	function update() {
		if (evalList.length === 0) return;
		evalIndex = positiveMod(evalIndex, evalList.length);
		repl_in.value = evalList[evalIndex];
	}
	
	function positiveMod(a, b) {
		return ((a%b)+b)%b;
	}	

	function parse(src) {
		if (src === undefined) {
			src = repl_in.value;
			repl_in.value = "";
		}
		show(src, JSON.stringify(parser.parse(src)));
	}

	function compile(src) {
		if (src === undefined) {
			src = repl_in.value;
			repl_in.value = "";
		}
		show(src, compiler.compile(src));
	}
				
	function evaluate(src) {
		if (src === undefined) {
			src = repl_in.value;
			repl_in.value = "";
		}
		if (!ready) {
			var msg = "The system is not initialized yet.";
			console.log(msg);
			show(src, msg);
			return;
		}
		try {
			var result = compiler.evaluate(src);
			console.log(result);
			show(src, result.receive('toString')().toString());
		} catch (ex) {
			console.log(ex);
			show(src, ex.toString());
		}
	}

	function show(expr, result) {
		if (expr.length > 0) {
			if (expr !== evalList[evalList.length - 1]) {
				evalList.push(expr);
			}
			evalIndex = evalList.length;
		}
		
		var p = document.createElement("p");
		split(expr).forEach(function (line, index) {				
			var exprDiv = document.createElement("div");
			exprDiv.setAttribute("class", "expr");
			exprDiv.textContent = (index == 0 ? ">>  " : "    ") + line;
			p.appendChild(exprDiv);
		});
		split(result).forEach(function (line) {				
			var resDiv = document.createElement("div");
			resDiv.setAttribute("class", "result");
			resDiv.textContent = line;
			p.appendChild(resDiv);
		});
		repl_out.insertBefore(p, repl_out.firstChild);
	}

	function split(str) {
		return str.match(/[^\r\n]+/g);
	}
	
	return {
		init: init,
		parse: parse,
		compile: compile,
		evaluate: evaluate,
		show: show,
		start: function () { ready = true; }
	};
})();