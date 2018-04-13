class Point {
	constructor(x, y) {
		this.x = x;
		this.y = y;
	}
	Clone() {
		return new Point(this.x, this.y);
	}
	get Length() {
		return Math.sqrt(this.x * this.x + this.y * this.y);
	}
	DistanceTo(b) {
		return Math.hypot(this.x - b.x, this.y - b.y);
	}
	Add(b) {
		this.x += b.x;
		this.y += b.y;
	}
	Sub(b) {
		this.x += b.x;
		this.y += b.y;
	}
	Scale(value) {
		this.x *= value;
		this.y *= value;
	}

}
class Particle {
	constructor() {
		this.location = new Point(0, 0);
		this.speed = new Point(0, 0);
		this.acceleration=new Point(0,0);
		this.radius = 5;
	}
	static at(x, y) {
		var t = new Particle();
		t.location.x = x;
		t.location.y = y;
		return t;

	}
	static build(loc, spd,accel, r) {
		var t = new Particle();
		t.location = loc;
		t.speed = spd;
		t.radius = r;
		t.acceleration=accel;
		return t;
	}
	Clone() {
		return Particle.build(this.location.Clone(), this.speed.Clone(),this.acceleration.Clone(), this.radius);
	}
	computeAcceleration(world) {
		let accel = new Point(0, 0);

	 
		let bounced = false;
		//potentialE = 0.0;
		if (this.location.x < this.radius) {
			this.location.x = this.radius;
			accel.x = -2 * this.speed.x;
			bounced = true;
		} else if (this.location.x > world.width - this.radius) {
			this.location.x = world.width - this.radius;
			accel.x = -2 * this.speed.x;
			bounced = true;
		}
		if (this.location.y < this.radius) {
			this.location.y = this.radius;
			accel.y = -2 * this.speed.y;
			bounced = true;
		} else if (this.location.y > world.height - this.radius) {
			this.location.y = world.height - this.radius;
			accel.y = -2 * this.speed.y;
			bounced = true;
		}
		if (bounced) { 
			return accel;
		}
		for (let p of world.points) {
			if (p != this) {
				let dx = this.location.x - p.location.x;
				let dx2 = dx * dx;
				let dy = this.location.y - p.location.y;
				let dy2 = dy * dy;
				let rSquared = dx2 + dy2;
				let rSquaredInv = 1.0 / rSquared;
				let attract = rSquaredInv * rSquaredInv * rSquaredInv;
				let repel = attract * attract;
				//potentialE += (4.0 * (repel - attract)) - pEatCutoff;
				let fOverR = 24.0 * ((2.0 * repel) - attract) * rSquaredInv;
				let fx = fOverR * dx;
				let fy = fOverR * dy;
				accel.x += fx; // add this force on to i's acceleration (m = 1)
				accel.y += fy;
				//ax[j] -= fx; // Newton's 3rd law
				//ay[j] -= fy; 
			}

		}
		return accel;
	}
	UpdateAccelAndSpeed(world) {
		var result = this.Clone();

		var accel = this.computeAcceleration(world);
		result.acceleration.Add(accel);		
		accel.Scale(world.halfdt);
		result.speed.Add(accel);
		return result;
	}
	GetPreState(world) {
		var result = this.Clone();
		result.location.x+=result.speed.x*world.dt + result.acceleration.x*world.halfdtsquared;
		result.location.y+=result.speed.y*world.dt + result.acceleration.y*world.halfdtsquared;
		result.speed.x+=result.acceleration.x*world.halfdt;
		result.speed.y+=result.acceleration.y*world.halfdt; 
		return result;
	}
}
class nbodyGraph {
	draw() {
		this.context.fillStyle = "black";
		this.context.fillRect(0, 0, 500, 500);
		for (let p of this.points) {
			this.context.fillStyle = "blue";
			this.context.beginPath();
			this.context.arc(p.location.x, p.location.y, p.radius, 0, 2 * Math.PI, true);
			this.context.fill();
		}
	}
	step() {
		let t = [];
		for (let p of this.points) {
			t.push(p.GetPreState(this));
		}
		this.points=t;
		 t=[];
		for (let p2 of this.points) {
			t.push(p2.UpdateAccelAndSpeed(this));
		}
		this.points = t;
	}
	constructor(canv, points) {
		this.width = canv.width;
		this.height = canv.height;
		this.context = canv.getContext("2d");
		this.points = points;
		this.dt= 0.01; 
		this.halfdt = 0.5 * this.dt;
		this.halfdtsquared = this.halfdt * this.halfdt;
	}

}
