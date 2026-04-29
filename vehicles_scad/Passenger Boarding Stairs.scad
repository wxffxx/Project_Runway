// Airport Passenger Boarding Stair Truck

$fn = 16;

module stair_truck() {
    translate([0, 0, 1.0])
        cube([8.0, 2.2, 0.8], center=true);

    //CAB

    translate([3.0, 0, 2.15])
        cube([2.0, 2.2, 1.5], center=true);

    //WHEELS
    wheel_r = 0.6;
    wheel_w = 0.5;
    
    for (x = [-2.5, 2.5]) {
        for (y = [-1.25, 1.25]) {
            translate([x, y, wheel_r])
                rotate([90, 0, 0])
                    cylinder(h=wheel_w, r=wheel_r, center=true);
        }
    }

    //PLATFORM
    translate([2.75, 0, 4.9])
        cube([2.5, 2.4, 0.2], center=true);

    //STAIR
    hull() {
        translate([-3.5, 0, 1.6])
            cube([1.0, 2.4, 0.4], center=true);

        translate([1.0, 0, 4.6])
            cube([1.0, 2.4, 0.4], center=true);
    }
}

stair_truck();