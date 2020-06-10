window.onload = (event) => {
    function create_UUID() {
        var dt = new Date().getTime();
        var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (dt + Math.random() * 16) % 16 | 0;
            dt = Math.floor(dt / 16);
            return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
        });
        return uuid;
    }
    if (!hive) {
        throw "hive library not found.";
    }
    let result = {};
    result.awarenessKind = {
        Any: 0b0,
        Presence: 0b1,
        Location: 0b10,
        Density: 0b100,
        UserInfo: 0b1000,
        Rol: 0b10000,
        ActivityLevel: 0b100000,
        Action: 0b1000000,
        History: 0b10000000,
        Intention: 0b100000000,
        Bookmark: 0b1000000000,
        Change: 0b10000000000,
        Expectation: 0b100000000000,
        Object: 0b1000000000000,
        Visibility: 0b10000000000000,
        Ability: 0b100000000000000,
        Influence: 0b1000000000000000,
        All: 0b1111111111111111
    };

    
    function ensureAwarenessInitialization(obj) {
        if (!obj["__awareness__"]) {
            obj["__awareness__"] = {
                subscriptions: {},
                history: []
            };
        }

    }

    result.subscribe = function (obj, kind, callback) {
        ensureAwarenessInitialization(obj);
        let me = create_UUID();
        obj["__awareness__"].subscriptions[me]
            = { kind: kind, events: [] };
        let cancelation = setInterval(() => {
            if(obj["__awareness__"].subscriptions[me].events.length!=0)
            {
                callback(obj["__awareness__"].subscriptions[me].events.shift());
            }
        }, 10);
        return { unsusbcibe: function (){
            clearInterval(cancelation);
            delete obj["__awareness__"].subscriptions[me];
        }};

    };
    result.notify = function (obj, kind, data) {
        ensureAwarenessInitialization(obj);
        let subscribers = obj["__awareness__"].subscriptions;
        for (const key in subscribers) {
            if (subscribers.hasOwnProperty(key)) {
                const element = subscribers[key];
                if((element.kind & kind) == element.kind)
                {
                    element.events.push(data);
                }
            }
        }
    };
    hive.awareness = result;
};