let hive = (function () {
	if (!firebase) {
		//firebase should be included.
		debugger;
	}
	if (!Proxy) {
		//i need proxy to work .
		debugger;
	}
	if(!Map)
	{
		//i need Map to work.
		debugger;
	}
	//set new method to Map
	Map.prototype.getKey = function (inputValue) {
		for (let[key, value]of this) {
			if (value == inputValue)
				return key
		}
		return undefined;
	}
	
	let module = {
		config: {
			apiKey: " AIzaSyA-Y_mz58xgvGkQNK_tQCXQiG3q1mlA6hM",
			authDomain: "hive-1336.firebaseapp.com",
			databaseURL: "https://hive-1336.firebaseio.com/",
			storageBucket: "hive-1336.appspot.com"
		}
	};
	let database = null;
	//this map contains key -> obj
	let loadedObjects = new Map();
	//this map contains root name -> key
	let roots = new Map();
	
	//this map contains obj -> proxy
	let proxies = new Map();
	//this map contains proxy -> handler
	let handlers = new Map();
	//this map contains id->queue
	let queues = new Map();
	
	module.start = function (callback) {
		roots.clear();
		loadedObjects.clear();
		firebase.initializeApp(module.config);
		database = firebase.database();
		handlers.forEach( function(value,key){
			unsuscribeProxy(key);			
		});
		proxies.clear();
		handlers.clear();
		
		queues.clear();
		database.ref("objects").once("value").then(function(objectsSnapshot){
			objectsSnapshot.forEach((i)=>{childAdded(i)});			
			database.ref("objects").on("child_added", childAdded);
			database.ref("objects").on("child_removed", childRemoved);
			database.ref("objects").on("child_changed", childChanged);
			database.ref("queues").once("value").then(function(queueSnapshot){
				queueSnapshot.forEach((i)=>{queueAdded(i)});
				database.ref("queues").on("child_added", queueAdded);
				database.ref("queues").on("child_changed", queueAdded);
				database.ref("queues").on("child_removed", queueRemoved);
				database.ref("roots").once("value").then(function(rootSnapshot){
					rootSnapshot.forEach((i)=>{rootAdded(i)});
					database.ref("roots").on("child_added", rootAdded);
					database.ref("roots").on("child_changed", rootAdded);
					database.ref("roots").on("child_removed", rootRemoved); 
					if(callback && callback.constructor.name=="Function"){
						callback();
					}
				});
			});	
		});
		
		return module;
	}

	module.remove = function (key) {
		database.ref("roots/" + key).set(null);
		doGC();
	}
	module.removeElement = function (proxy) {
		//the obj i get here should be a proxy.
		let id = loadedObjects.getKey(proxies.getKey(proxy));
		if (id) {
			let key = roots.getKey(id);
			if(key){
				module.remove(key);
			} 
		}
	}
	module.set =function(key,obj){
		if(key.constructor.name != "String" )
		{
			throw "The key must be a string ";
		}
		let id = loadedObjects.getKey(obj);
		if (!id) {
			id=innerAdd(obj);
		} 
		database.ref("roots/" + key).set(id);
		return getProxy(obj); 
	};
	module.get=function(key){
		if(!roots.has(key))
		{throw "Key not found in Hive Roots";}
		return getProxy(loadedObjects.get(roots.get(key)));
	};
	module.keys=function(){
		return Array.from(roots.keys());
	};
	module.elements=function(){
		let result=new Map();
		 Array.from(roots.keys()).forEach(function(key){
			result.set(key,getProxy( loadedObjects.get(roots.get(key)))); 
		}); 
		return result;
	};
	
	module.forEach=function(callback){
		Array.from(roots.keys()).forEach(function(key){
			callback(key,getProxy(loadedObjects.get(roots.get(key))));
		}); 
		
		return module;
	};
	//internal stuff
	function innerAdd (obj){
		let id = loadedObjects.getKey(obj);
		if (id) {
			return id;
		} 
		let key = database.ref("objects").push().key;
		loadedObjects.set(key, obj);
		if(obj==null || obj==undefined)
		{
			let upd={}; 
			upd["/" + key + "/type/"]="null";
			upd["/" + key + "/value/"]=null;
			database.ref("objects").update(upd);
			
		}
		else if(!isPrimitive(obj)){
			//now there is a thing, what if this is a primitive object, it needs boxing and unboxing.
			updateFields(obj,Object.keys(obj));
		}else{
			let upd={};
			var type=obj.constructor.name;
			upd["/" + key + "/type/"]=type;
			let basePath = "/" + key + "/";
			//first of all i need to see if the value is either null or undefined.
			if (type=="Date"){
				upd[basePath+"value"]=obj.toJSON();
			}else {
				upd[basePath + "value"] = obj;
			}
			database.ref("objects").update(upd);
		}
		
		return key;
	};


	//this is to hold the unloaded childs.
	let missingReferences=[];
	
	function checkForRefrences(key, obj) {
		//to use.
		let toExecute = missingReferences.filter(function (item) {
				return item.key == key;
			});
		missingReferences = missingReferences.filter(function (item) {
				return item.key != key;
			});
		toExecute.forEach(function (item) {
			item.action(obj);
		});
	}
	
	function childAdded(dataSnapshot) {
		if (!loadedObjects.has(dataSnapshot.key)) {
			let obj = null;
			let received = dataSnapshot.val();
			if(isPrimitiveTypeName(received.type)){
				if(received.type == "Date"){
					obj=new Date(received.value);
				}else{
					//let's say this is a literal for now.
					obj = received.value; 
				}
				loadedObjects.set(dataSnapshot.key,obj);
			}else if(received.type==="null"){
				loadedObjects.set(dataSnapshot.key,null);
			}else{
				if(eval("typeof("+received.type+")")!="undefined"){
					obj=eval("new "+received.type+"();");
				}else{obj={};}
				loadedObjects.set(dataSnapshot.key, obj);
				mapSnapshotToObject(obj,received);
			}
			
			checkForRefrences(dataSnapshot.key, obj);
		}
	}

	function childRemoved(oldDataSnapshot) { 
		let obj= loadedObjects.get(oldDataSnapshot.key);
		unsuscribeProxy(proxies.get(obj));
		handlers.delete(proxies.get(obj));
		proxies.delete(obj); 
		loadedObjects.delete (oldDataSnapshot.key);
	}
	function childChanged(dataSnapshot) {
//todo: this has some issues if i have an array and transform it into an object or something like that.
		let obj = loadedObjects.get(dataSnapshot.key);
		let received = dataSnapshot.val();
		mapSnapshotToObject(obj,received);
	}
	
	function mapSnapshotToObject(obj,received)	{
		//i need to remove everything that is in the object and was not sent.
		for(let k in obj){
			if( !("data" in received) || ! (k in received.data)){
				//i HAVE a key that is not on the received data.
				delete obj[k];
			}
		}
		for (let k in received.data) {
			if (received.data[k] != null) {
				if (received.data[k].type=="null"){
					//i have a null here
					obj[k]=null;
				}else if(isPrimitiveTypeName(received.data[k].type)){
					if(received.data[k].type == "Date"){
						obj[k]=new Date(received.data[k].value);
					}else{
						//let's say this is a literal for now.
						obj[k] = received.data[k].value; 
					}					
				} else if(received.data[k].type == "Function"){
					debugger;
				}else{ //object or array
					//if the object is not in my cache, i might have some sync issues here.
					let other = loadedObjects.get(received.data[k].value);
					if (!other) {
						missingReferences.push({
							key: received.data[k].value,
							action: function (child) {
								obj[k] = child;
							}
						});
					}
					if (obj[k] != other) {
						obj[k] = other;
					}
				}
			}
		}
	}
	function updateFields(obj,fieldNames){
		let upd = {};
		let id = loadedObjects.getKey(obj);
		if (id) {
			upd["/" + id + "/type/"]=obj.constructor.name;
			fieldNames.forEach(function(fieldName) 
			{
				let basePath = "/" + id + "/data/" + fieldName + "/";
				//first of all i need to see if the value is either null or undefined.
				let value=obj[fieldName];
				if(value==null || value==undefined)
				{
					obj[fieldName]=null;//this is just to ensure that no undefined is left here.
					upd[basePath+"type"]="null";
					upd[basePath+"value"]=null;
				}else{
					let type = value.constructor.name;
					upd[basePath + "type"] = type;
					if(isPrimitive(value))
					{
						if (type=="Date"){
							upd[basePath+"value"]=value.toJSON();
						}else {
							upd[basePath + "value"] = value;
						}
					}else{
						//sanitize the object to ensure no proxies are really in it, this object should have a reference to the real thing
						if(handlers.has(obj[fieldName]))
						{
							obj[fieldName]=proxies.getKey(value);
							value=obj[fieldName];
						}
						if (type == "Object" || type == "Array") {
							upd[basePath + "value"] = innerAdd(value);
							} else {debugger;}
					}
				}
		
			} );
			database.ref("objects").update(upd);
		}
	}
	function removeElementsFromArray(obj,oldLength)	{
		let upd = {};
		let id = loadedObjects.getKey(obj);
		if (id) {
			let basePath = "/" + id + "/data/";
			for(let i = oldLength-1; i>obj.length-1; i--)
			{
				upd[basePath+i]=null;
			}
			database.ref("objects").update(upd);
		}
	}
	function removeField(obj,fieldName){
		let upd = {};
		if(handlers.has(obj))
		{
			//they gave me a proxy.
			obj=proxies.getKey(obj);
		}
		let id = loadedObjects.getKey(obj);
		if (id) {
			let basePath = "/" + id + "/data/";
			upd[basePath+fieldName]=null;
			database.ref("objects").update(upd);
		}
		
	}
	function updateField(obj, fieldName) {
		updateFields(obj,[fieldName]);
	}
	
	function rootAdded(dataSnapshot){
		 roots.set(dataSnapshot.key,dataSnapshot.val());	 
	}
	function rootRemoved(oldDataSnapshot) {
		roots.delete(oldDataSnapshot.key);
	} 
	function isPrimitive(obj)	{
		if(obj==null || obj==undefined)
		{return false;}
		return isPrimitiveTypeName(obj.constructor.name);
	}
	function isPrimitiveTypeName(name)	{
		return name=="Number" || 
			name=="Date" ||
			name=="Boolean" ||
			name=="String" ;
	}
	function getProxy(obj)	{		
		if(obj==null)
		{return obj;}
		if(isPrimitive(obj))
		{return obj;}
		if(proxies.has(obj))
		{
			let proxy=proxies.get(obj);
			let handler=handlers.get(proxy);
			//ensure the handler is enabled.
			if(!handler.get)
			{
				handler.get=getExecuted;
				if(obj.constructor.name=="Array")
				{
					handler.set=arraySetExecuted;
				}else{
					handler.set=setExecuted;
				}
			}
			return proxy;
		} 
		//create handler
		let handler = {get:getExecuted,set:setExecuted};
		if(obj.constructor.name=="Array")
		{
			handler.set=arraySetExecuted;
		}
		let proxy= new Proxy(obj,handler);
		proxies.set(obj,proxy);
		handlers.set(proxy,handler);
		return proxy;
	}
	function unsuscribeProxy(proxy)	{
		if(handlers.has(proxy))
		{
			let handler = handlers.get(proxy);
			handler.get=undefined;
			handler.set=undefined;
		}
	}
	
	function getExecuted(target,property,rcvr)	{
		return getProxy(target[property]); 
	}
	function setExecuted(target,property,value,rcvr)	{ 
		//if what they are setting is a proxy, i need to actually set the real object.
		if(handlers.has(value))
		{
			value=proxies.getKey(value);
		}
		//if i have something null/undefined, then valueOf will fail
		if(target[property]== null ||target[property]== undefined 
			||value== null||value==undefined){
				//if they are different.
				if(target[property]!=value|| !(property in target))
				{
					target[property]=value;
					updateField(target, property);
				}
			}
		else if (target[property].valueOf() != value.valueOf()) {
				target[property]=value;
				updateField(target, property);
			}
		return true;
	}
	function arraySetExecuted(target,property,value,rcvr)	{
		//if what they are setting is a proxy, i need to actually set the real object.
		if(handlers.has(value))
		{
			value=proxies.getKey(value);
		}
		//if the Length is being set under my current length, it is a delete
		if(property=="length")
		{
			let oldLength=target[property];	
			target[property]=value;
			if(value<oldLength)
			{
				//removed elements.
				removeElementsFromArray(target,oldLength);
			}
		}else{
			//i have set a value into the array.
			target[property]=value;
			updateField(target,property);
		}
		return true;
	}
	
	
	
	//GC
	let initializedGC=false;
	function doGC(){
		if(!initializedGC)
		{
			initializedGC=true;		
			//i am going to list all the elements i should actually delete.
			let untouchedSet=new Set(loadedObjects.keys());
			setTimeout(function(){ 
				roots.forEach(function(value,key){mark(loadedObjects.get(value),untouchedSet);});
				queues.forEach(function(value,key){mark(loadedObjects.get(value),untouchedSet);});
				sweep(untouchedSet);	 
				initializedGC=false;
			}, 2000);	
			
		}
	};
	function mark(obj,untouchedSet)	{
		if(obj!=undefined)
		{
			let type = obj.constructor.name;
			if(isPrimitiveTypeName(type))
			{
				//do nothing
			}else if(type=="Function")
			{
				//do nothing for now, if i have ever a clojure this should change.
			}
			else if(type=="Array") {
				let id = loadedObjects.getKey(obj);
				if(id){
					if(!untouchedSet.has(id)){
						//if i have already visited this node, do nothing.
						return;
					} 
					untouchedSet.delete(id);
					obj.forEach(function(item){
						mark(item,untouchedSet);
					});
				}
			}else{ //object.
				let id = loadedObjects.getKey(obj);
				if (id) {
					if(!untouchedSet.has(id)){
						//if i have already visited this node, do nothing.
						return;
					} 
					untouchedSet.delete(id);
					for (let k in obj) {
						if (obj[k]) {
							mark(obj[k],untouchedSet);
						}
					}
				} 
			}  
		}
	};
	function sweep(untouchedSet){
		let upd = {};
		//todo: optimize.
		untouchedSet.forEach(function(key){
			upd["/"+key]=null; 
		});
		database.ref("objects").update(upd);
	};
	//GC 
	
	//lock
	function innerLock(pxy,callback,lockChain,then)	{
		//the object provided SHOULD be a proxy
		let obj=pxy; 
		if(handlers.has(pxy))
		{
			obj=proxies.getKey(pxy);
		}else{
			throw "The object to be locked must be shared ";
		}
		let id = loadedObjects.getKey(obj);
		if (!id) { 
			//if the object is not shared this is just useless i guess
			//but i think i should do something like this. id=innerAdd(obj);
			throw "The object to be locked must be shared ";
		}
		if(acquiredLocks.has(id)){
			//re-entrant part, if i own this lock, i just execute
			callback();
			if(then )
			{then();}
			return;
		}
		lockChain.add(id);
		//try to get the lock
		database.ref("locks/"+id).transaction(function(data){
			if(data==null)
			{
				//no one owns this! yay!
				return true;
			}else{
				return; //this aborts my transaction
			}
			
		},function(error, committed, snapshot){
			if(!committed || error){
				//i aborted the transaction, that means that
				//somebody has this lock, must retry
				
				//this specific call might be an issue. check it later
				setTimeout(function(){innerLock(pxy,callback,lockChain,then);},10);  
			}else{
				//i committed the transaction, this means i own the lock
				acquiredLocks.add(id);
				let count = lockChain.size;
				let newLock = function (pxy2,cb2){
					createLock(pxy2,cb2,lockChain);
				}
				let oldLock=hive.lock;
				hive.lock=newLock;
				callback();
				hive.lock=oldLock;
				if(count ==lockChain.size)
				{
					let arr=  Array.from(lockChain);
						for (let i = 0, len = arr.length; i < len; i++) {
						//todo: make this a mass update.
						let k = arr[i];
						acquiredLocks.delete(k);
						database.ref("locks/"+k).set(null);
					}
				}
				if(then)
				{
					then();
				}
			}
			
		},
		false /*this is just so the db does not raise local events in case the transaction fails*/
		/*see https://firebase.google.com/docs/reference/js/firebase.database.Reference#transaction */);
		
	}
	let acquiredLocks = new Set();
	function createLock(pxy, callback, set){
		return new Promise(function(resolve,reject){
			innerLock(pxy,callback,set,resolve); 
		});
	}
	module.lock = function(pxy,callback){
		return createLock(pxy,callback,new Set());
	};
	
	module.sync=function(pxy,callback){
			//the object provided SHOULD be a proxy
		let obj=pxy; 
		if(handlers.has(pxy))
		{
			obj=proxies.getKey(pxy);
		}else{
			throw "The object to be locked must be shared ";
		}
		let id = loadedObjects.getKey(obj);
		if (!id) { 
			//if the object is not shared this is just useless i guess
			//but i think i should do something like this. id=innerAdd(obj);
			throw "The object to be locked must be shared ";
		}
		var updateAndExecute = function(cb){
			database.ref("objects/"+id).once("value").then(function(objectSnapshot){
				childChanged(objectSnapshot);
				callback();
				if(cb)
				{
					cb();
				}
			});
		};
		if(acquiredLocks.has(id)){
			//re-entrant part, if i own this lock, i just execute
			updateAndExecute();
			return;
		}
		//try to get the lock
		database.ref("locks/"+id).transaction(function(data){
			if(data==null)
			{
				//no one owns this! yay!
				return true;
			}else{
				return; //this aborts my transaction
			}
			
		},function(error, committed, snapshot){
			if(!committed || error){
				//i aborted the transaction, that means that
				//somebody has this lock, must retry
				setTimeout(function(){module.lock(pxy,callback);},10);  
			}else{
				//i committed the transaction, this means i own the lock
				acquiredLocks.add(id);
				updateAndExecute(function(){
					acquiredLocks.delete(id);
					database.ref("locks/"+id).set(null);
				});
			}
			
		},
		false /*this is just so the db does not raise local events in case the transaction fails*/
		/*see https://firebase.google.com/docs/reference/js/firebase.database.Reference#transaction */); 
	};
	//lock
	
	function queueAdded(dataSnapshot){
		 queues.set(dataSnapshot.key,dataSnapshot.val());	
	} 
	function queueRemoved(oldDataSnapshot) {
		queues.delete(oldDataSnapshot.key);
	} 
	//processQueue
	function getQueue(key){
		//this should lock and be async.
		if(!queues.has(key))
		{
			let obj={'req':{},'rsp':{}};
			let id =innerAdd(obj);
			database.ref("queues/" + key).set(id);
		}
		return getProxy(loadedObjects.get(queues.get(key))); 
	}  
	module.request=function(key,data){
		return new Promise(function(resolve,reject){
			
		if(key.constructor.name != "String" )
		{
			reject("The key must be a string ");
		}else{			
			let q = getQueue(key);
			let rId = loadedObjects.getKey(q.req);
			let rk= database.ref("objects/"+ rId).push().key;
			q.req[rk]=data; 
			
			var cb = function(){
				if(rk in q.rsp){
					//i have an answer! 
					let response=q.rsp[rk];
					removeField(q.rsp,rk); 
					resolve(response);
				}else{
					//async retry
					setTimeout(cb,10);
				}
				
			}
			cb();
		}});
	};
	 
	module.process=function(key,func){
		return new Promise(function(resolve,reject){
			if(key.constructor.name != "String" ){
				reject("The key must be a string ");
			}else{
				let q = getQueue(key);
				//maybe this check for size to avoid unnecesary locks
				//Object.keys(q.req).length
				
				//i want to UNLOCK this stuff before i start executing func!
				//so look at t as a auto-retrying async like function
				
				let rk=null;
				let data=null;
				let getData = function(){
					let keys1 = Object.keys(q.req);
					if(keys1.length==0){
						setTimeout(getData,10);
					}else{
						module.lock(q.req,function(){
						//i own the lock, so now what?
						let keys = Object.keys(q.req);
						if(keys.length==0){
							//nothing to do? retry in a while
							setTimeout(getData,10);
						}else{
							//i remove the first item from the queue.
							rk=keys[0];
							data=q.req[rk];
							removeField(q.req,rk); 
						}});
					}

				};
				let processData = function(){
					if(data==null){
						setTimeout(processData,10);  
					}else{
						//i have a thing to process.
						var result = q.rsp[rk]=func(data);
						resolve(result);
					}
				};
				getData();
				processData();
			}
		}); 
	};
	
	module.requestAll=function(key,data){
		return Promise.all(
		data.map(d=> module.request(key,d))
		);
	}
	//processQueue
	return module;
}
	());
