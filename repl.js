
var REPL = (function () {
	
	var repl_in;
	var repl_out;
	var ready = false;	
	
	function init(in_textArea, out_div) {
		repl_in = in_textArea;
		repl_out = out_div;
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
		if (!ready) {
			var msg = "The system is not initialized yet.";
			console.log(msg);
			show(src, msg);
			return;
		}
		if (src === undefined) {
			src = repl_in.value;
			repl_in.value = "";
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