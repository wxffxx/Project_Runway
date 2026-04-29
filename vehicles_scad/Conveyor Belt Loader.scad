// Airport Baggage Belt Loader

module baggage_belt_loader() {
    $fn = 16;

    wheel_r = 0.4;
    wheel_w = 0.4;
    wheel_x = [-3.0, 0.0];
    wheel_y = [-0.8, 0.8]; 

    chassis_l = 5.0;
    chassis_w = 1.6;
    chassis_h = 0.6;
    chassis_x = -1.5;
    chassis_z = 0.7;

    // Wheel
    for (x = wheel_x) {
        for (y = wheel_y) {
            translate([x, y, wheel_r])
            rotate([90, 0, 0])
            cylinder(h=wheel_w, r=wheel_r, center=true);
        }
    }

    // main body
    translate([chassis_x, 0, chassis_z])
    cube([chassis_l, chassis_w, chassis_h], center=true);

    // Driver Cab and Control Box
    translate([-3.2, 0, 1.25])
    cube([1.2, 1.4, 0.5], center=true);

    // Conveyor
    translate([-1.5, 0, 1.0])
    rotate([0, -25, 0])
    translate([1.85, 0, 0.1]) // Offsets the belt to reach forward and slightly backward
    cube([7.7, 1.2, 0.2], center=true);
}

baggage_belt_loader();