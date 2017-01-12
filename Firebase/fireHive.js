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
		config: {
			apiKey: " AIzaSyA-Y_mz58xgvGkQNK_tQCXQiG3q1mlA6hM",
			authDomain: "hive-1336.firebaseapp.com",
			databaseURL: "https://hive-1336.firebaseio.com/",
			storageBucket: "hive-1336.appspot.com"
		}
	};
	var database = null;
	var loadedObjects = new Map();

	module.start = function () {
		loadedObjects.clear();
		firebase.initializeApp(module.config);
		database = firebase.database();
		database.ref("test").on("child_added", childAdded);
		database.ref("test").on("child_removed", childRemoved);
		database.ref("test").on("child_changed", childChanged);
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
		var key = database.ref("test").push().key;
		loadedObjects.set(key, obj);
		var data = {};
		for (var k in obj) {
			if (obj[k] != null) {
				data[k] = {
					type: obj[k].constructor.name
				};
				if (data[k].type == "Object") {

					data[k].value = module.add(obj[k]);
				} else if (data[k].type != "Function") {
					data[k].value = obj[k];
				} else {
					//to do.
					debugger;

				}
			}
		}
		//watching.

		try {
			enableWatch(obj);
			database.ref("test/" + key).set(data);
		} catch (err) {
			//on error i have to remove it from the local cache.
			unwatch(obj);
			loadedObjects.delete (key);
			throw (err);
		}

		return key;
	}

	module.elements = function () {
		var result = [];
		loadedObjects.forEach(function (item) {
			result.push(item);
		})
		return result;
	}
	module.forEach = function (callback) {
		loadedObjects.forEach(function (item, key) {
			callback(key, item);
		});
		return module;
	}
	//internal stuff
	//this is to hold the unloaded childs.
	var missingReferences = [];
	function childAdded(dataSnapshot) {
		if (!loadedObjects.has(dataSnapshot.key)) {
			var obj = {};
			var received = dataSnapshot.val();
			loadedObjects.set(dataSnapshot.key, obj);
			for (var k in received) {
				if (received[k] != null) {
					if (received[k].type == "Object") {
						//if the object is not in my cache, i might have some sync issues here.
						var other = loadedObjects.get(received[k].value);
						if (!other) {
							//i am suposed to have a reference to an object that i do not have yet.
							//this is either because i got a dangling pointer  of some sorts, or because the object is about to arrive.
							missingReferences.push({
								key: received[k].value,
								action: function (child) {
									obj[k] = child;
								}
							});
						}
						obj[k] = other;
					} else if (received[k].type == "Function") {
						//you should not be here.
						debugger;
					} else {
						//let's say this is a literal for now.
						obj[k] = received[k].value;
					}
				}
			}
			enableWatch(obj);
			checkForRefrences(dataSnapshot.key, obj);
		}
	}
	function checkForRefrences(key, obj) {
		//to use.
		var toExecute = missingReferences.filter(function (item) {
				return item.key == key;
			});
		missingReferences = missingReferences.filter(function (item) {
				return item.key != key;
			});
		toExecute.forEach(function (item) {
			item.action(obj);
		});
	}
	function enableWatch(obj) {
		watch(obj,
			function (fieldName, operation, newValue, oldValue) {
			if (newValue != oldValue) {
				updateField(this, fieldName);
			}
		}, 0 //this is to prevent it to crawl the object. only attributes that are local to it will trigger the event
		);
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
						missingReferences.push({
							key: received[k].value,
							action: function (child) {
								obj[k] = child;
							}
						});
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

	function updateField(obj, fieldName) {
		var upd = {};
		var id = loadedObjects.getKey(obj);
		if (id) {
			var basePath = "/" + id + "/" + fieldName + "/";
			var type = obj[fieldName].constructor.name;

			upd[basePath + "type"] = type;
			if (type == "Object") {
				upd[basePath + "value"] = module.add(obj[fieldName]);
			} else {
				upd[basePath + "value"] = obj[fieldName];
			}
			database.ref("test").update(upd);
		}
	}

	return module;
}
	().start());
