var hive = (function () {
	if (!firebase) {
		//firebase should be included.
		debugger;
	}	
	if (!watch) {
		//firebase should be included.
		debugger;
	}
	//set new method to Map
	Map.prototype.getKey = function (inputValue) {
		for (var[key, value]of this) {
			if (value == inputValue)
				return key
		}
		return undefined;
	}
	var module = {
		config : {
			apiKey : " AIzaSyA-Y_mz58xgvGkQNK_tQCXQiG3q1mlA6hM",
			authDomain : "hive-1336.firebaseapp.com",
			databaseURL : "https://hive-1336.firebaseio.com/",
			storageBucket : "hive-1336.appspot.com"
		}
	};
	var database = null;
	var loadedObjects = new Map();

	module.start = function () {
		loadedObjects.clear();
		firebase.initializeApp(module.config);
		database = firebase.database();
		return module;
	}

	module.remove = function (obj) {
		var id = loadedObjects.getKey(obj);
		if (id) {
			database.ref('test/' + id).set(null);
			return id;
		}
		return null;
	}
	module.add = function (obj) {
		var id = loadedObjects.getKey(obj);
		if (id) {
			return id;
		}
		//watching.

		enableWatch(obj);
		var key = database.ref("test").push().key;
		loadedObjects.set(key, obj);
		var data = {};
		for (var k in obj) {
			if (obj[k] != null) {
				data[k] = {
					type : obj[k].constructor.name
				};
				if (data[k].type == "Object") {

					data[k].value = Add(obj[k]);
				} else if (data[k] != "Function") {
					data[k].value = obj[k];
				} else {}
			}
		}
		database.ref("test/" + key).set(data);
		return key;
	}

	module.elements=function()
	{
		var result=[];
		loadedObjects.foreach(function(item){result.push(item);})
		return result;
	}
	module.foreach=function(callback)
	{
		loadedObjects.foreach(function (item,key){callback(key,item);});
		return module;
	}
//internal stuff
	function childAdded(dataSnapshot) {
		if (!loadedObjects.has(dataSnapshot.key)) {
			var obj = {};
			var received = dataSnapshot.val();
			for (var k in received) {
				if (received[k] != null) {
					if (received[k].type == "Object") {
						//if the object is not in my cache, i might have some sync issues here.
						var other = loadedObjects.get(received[k].value);
						if (!other) {
							debugger;
						}
						obj[k] = other;
					} else {
						//let's say this is a literal for now.
						obj[k] = received[k].value;
					}
				}
			}
			loadedObjects.set(dataSnapshot.key, obj);
			enableWatch(obj);
		}
	}

	function enableWatch(obj) {
		watch(obj,
			function (fieldName, operation, newValue, oldValue) {
			if (newValue != oldValue) {
				UpdateField(this, fieldName);
			}
		});
	}
	function childRemoved(oldDataSnapshot) {
		unwatch(loadedObjects.get(oldDataSnapshot.key));
		loadedObjects.delete (oldDataSnapshot.key);
	}
	function childChanged(dataSnapshot) {
		var obj = loadedObjects.get(dataSnapshot.key);
		var received = dataSnapshot.val();
		for (var k in received) {
			if (received[k] != null) {
				if (received[k].type == "Object") {
					//if the object is not in my cache, i might have some sync issues here.
					var other = loadedObjects.get(received[k].value);
					if (!other) {
						debugger;
					}
					if (obj[k] != other) {
						obj[k] = other;
					}
				} else {
					//let's say this is a literal for now.
					if (obj[k] != received[k].value) {
						//	unwatch(obj);
						obj[k] = received[k].value;
						//	enableWatch(obj);
					}
				}
			}
		}
	}

	function UpdateField(obj, fieldName) {
		var upd = {};
		var id = loadedObjects.getKey(obj);
		if (id) {
			var basePath = "/" + id + "/" + fieldName + "/";
			var type = obj[fieldName].constructor.name;

			upd[basePath + "type"] = type;
			if (type == "Object") {
				upd[basePath + "value"] = Add(obj[fieldName]);
			} else {
				upd[basePath + "value"] = obj[fieldName];
			}
			database.ref("test").update(upd);
		}
	}
	database.ref("test").on("child_added", childAdded);
	database.ref("test").on("child_removed", childRemoved);
	database.ref("test").on("child_changed", childChanged);
	return module;
}().start());