// Airport Baggage Tug and Carts

$fn = 16; 
module wheel_axle() {
    color("DarkSlateGray")
    translate([0, 0, 0.4])
    rotate([90, 0, 0])
    cylinder(h=2.0, r=0.4, center=true);
}

module tug() {
    //Body
    color("Gold")
    translate([0, 0, 0.7])
    cube([3.5, 1.8, 0.6], center=true);

    // cabin
    color("White")
    translate([-0.5, 0, 1.4]) // to rear of tractor
    cube([1.5, 1.6, 0.8], center=true);

    // roof
    color("DarkGray")
    translate([-0.5, 0, 1.85])
    cube([1.6, 1.7, 0.1], center=true);

    // wheels
    translate([1.0, 0, 0]) wheel_axle();
    translate([-1.0, 0, 0]) wheel_axle();
}

module cart() {
    // frame
    color("LightGray")
    translate([0, 0, 0.65])
    cube([3.5, 1.8, 0.15], center=true);

    // walls
    color("Silver")
    translate([1.65, 0, 0.95])
    cube([0.2, 1.8, 0.45], center=true);
    
    color("Silver")
    translate([-1.65, 0, 0.95])
    cube([0.2, 1.8, 0.45], center=true);

    // Wheeld
    translate([1.0, 0, 0]) wheel_axle();
    translate([-1.0, 0, 0]) wheel_axle();
}

// luggage
module baggage_load_1() {
    color("Tomato")
    translate([0.5, 0.2, 1.0])
    cube([1.2, 1.0, 0.55], center=true);

    color("SteelBlue")
    translate([-0.6, -0.1, 0.95])
    cube([0.8, 1.2, 0.45], center=true);
}

module baggage_load_2() {
    color("MediumSeaGreen")
    translate([0, 0, 1.05])
    cube([1.8, 1.4, 0.65], center=true);
}

module airport_baggage_train() {
    
    // tug
    translate([4.25, 0, 0]) tug();

    translate([0, 0, 0]) {
        cart();
        baggage_load_1();
    }

    translate([-4.25, 0, 0]) {
        cart();
        baggage_load_2();
    }
}

airport_baggage_train();