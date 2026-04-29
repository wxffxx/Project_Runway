// Airport Aviation Fuel Tanker Truck

module aviation_fuel_truck() {
    wheel_r = 0.6;
    wheel_w = 0.6;
    
    chassis_l = 9.8;
    chassis_w = 2.8;
    chassis_h = 0.5;
    
    cab_l = 2.5;
    cab_w = 2.8;
    cab_h = 1.8;
    
    tank_l = 6.8;
    tank_r = 1.2;

    // CHASSIS
    color("darkslategrey")
    translate([0, 0, wheel_r + chassis_h/2])
        cube([chassis_l, chassis_w, chassis_h], center=true);

    // WHEELS
    color("black") {
        // Front
        translate([3.5, 1.5 - wheel_w/2, wheel_r]) 
            rotate([90, 0, 0]) cylinder(h=wheel_w, r=wheel_r, center=true, $fn=20);
        translate([3.5, -1.5 + wheel_w/2, wheel_r]) 
            rotate([90, 0, 0]) cylinder(h=wheel_w, r=wheel_r, center=true, $fn=20);

        // Rear axle 1
        translate([-1.8, 1.5 - wheel_w/2, wheel_r]) 
            rotate([90, 0, 0]) cylinder(h=wheel_w, r=wheel_r, center=true, $fn=20);
        translate([-1.8, -1.5 + wheel_w/2, wheel_r]) 
            rotate([90, 0, 0]) cylinder(h=wheel_w, r=wheel_r, center=true, $fn=20);

        // Rear axle 2
        translate([-3.6, 1.5 - wheel_w/2, wheel_r]) 
            rotate([90, 0, 0]) cylinder(h=wheel_w, r=wheel_r, center=true, $fn=20);
        translate([-3.6, -1.5 + wheel_w/2, wheel_r]) 
            rotate([90, 0, 0]) cylinder(h=wheel_w, r=wheel_r, center=true, $fn=20);
    }

    // CAB
    color("white")
    translate([chassis_l/2 - cab_l/2, 0, wheel_r + chassis_h + cab_h/2])
        cube([cab_l, cab_w, cab_h], center=true);

    // windows
    color("skyblue")
    translate([chassis_l/2 - cab_l/2 + 0.1, 0, wheel_r + chassis_h + cab_h/2 + 0.2])
        cube([cab_l, cab_w + 0.1, cab_h - 0.6], center=true);

    // fuel tank
    color("silver")
    translate([-chassis_l/2 + tank_l/2 + 0.2, 0, wheel_r + chassis_h + tank_r])
        rotate([0, 90, 0])
        cylinder(h=tank_l, r=tank_r, center=true, $fn=30);
}

aviation_fuel_truck();