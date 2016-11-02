
var loaded = false;

function repl_parse(src) {
	if (src === undefined) {
		src = repl_in.value;
		repl_in.value = "";
	}
	repl_show(src, JSON.stringify(parser.parse(src)));
}

function repl_compile(src) {
	if (src === undefined) {
		src = repl_in.value;
		repl_in.value = "";
	}
	repl_show(src, compiler.compile(src));
}
			
function repl_eval(src) {
	if (!loaded) {
		console.log("The system is not initialized yet.");
		repl_show(src, "The system is not initialized yet.");
		return;
	}
	if (src === undefined) {
		src = repl_in.value;
		repl_in.value = "";
	}
	try {
		repl_show(src, compiler.evaluate(src).receive('toString')().toString());
	} catch (ex) {
		console.log(ex);
		repl_show(src, ex.toString());
	}
}

function repl_show(expr, result) {
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