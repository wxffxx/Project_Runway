// Airport Crash Tender / ARFF Fire Truck

module ARFF_FireTruck() {
    length = 12;
    width = 3.5;
    
    c_body = "firebrick";
    c_cab = "darkred";
    c_glass = "lightskyblue";
    c_tire = "#222222";
    c_silver = "silver";

    //LOWER
    color(c_body)
    translate([0, 0, 1.25])
        cube([length, width, 1.5], center = true);

    // rear tank & housing
    color(c_body)
    translate([-1.5, 0, 3.0])
        cube([9, width, 2.0], center = true);

    // front cab
    color(c_cab)
    translate([4.5, 0, 2.75])
        cube([3, width, 1.5], center = true);

    // windshield
    color(c_glass) {

        translate([5.96, 0, 2.95])
            cube([0.1, width - 0.4, 0.9], center = true);
            
        translate([4.5, 0, 2.95])
            cube([1.5, width + 0.02, 0.7], center = true);
    }

    // water shooter
    color(c_silver) {
        // base (4.0 ~ 4.25)
        translate([1.5, 0, 4.0])
            cylinder(h = 0.25, r = 0.5, $fn = 16);
            
        // barrel
        translate([2.2, 0, 4.375])
            cube([2.0, 0.3, 0.25], center = true);
    }

    // wheels
    wheel_r = 0.75;
    tire_w = 0.8;

    axles = [-4.25, -1.25, 4.25];
    y_offset = 1.35; 

    for (x = axles) {
        // left Wheels
        // tires
        color(c_tire)
        translate([x, y_offset, wheel_r])
            rotate([90, 0, 0])
                cylinder(h = tire_w, r = wheel_r, center = true, $fn = 24);
        // hubcaps
        color(c_silver)
        translate([x, y_offset - 0.01, wheel_r])
            rotate([90, 0, 0])
                cylinder(h = tire_w + 0.02, r = wheel_r * 0.5, center = true, $fn = 12);

        // Right Wheels
        // tires
        color(c_tire)
        translate([x, -y_offset, wheel_r])
            rotate([90, 0, 0])
                cylinder(h = tire_w, r = wheel_r, center = true, $fn = 24);
        // hubcaps
        color(c_silver)
        translate([x, -y_offset + 0.01, wheel_r])
            rotate([90, 0, 0])
                cylinder(h = tire_w + 0.02, r = wheel_r * 0.5, center = true, $fn = 12);
    }
}

ARFF_FireTruck();