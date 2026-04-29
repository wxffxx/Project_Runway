// Commercial Narrow-Body Airliner

module airliner() {
    $fn = 36; 

    c_body = "white";
    c_wing = "silver";
    c_engine = "darkgray";
    c_strut = "gray";
    c_tire = "black";

    // FUSELAGE
    color(c_body)
    hull() {
        // Nose Cone
        translate([15, 0, 4.5]) scale([1.5, 1, 1]) sphere(r=1.8);
        
        // Main Cabin
        translate([10, 0, 4.5]) sphere(r=2);
        translate([-10, 0, 4.5]) sphere(r=2);
        
        // Tail Cone
        translate([-16, 0, 5.5]) scale([1.5, 0.8, 0.8]) sphere(r=0.8);
    }

    // MAIN WINGS 
    color(c_wing)
    for (i = [-1, 1]) {
        scale([1, i, 1]) {
            hull() {
                // Wing Root
                translate([2, 1.8, 3.8]) sphere(r=0.6);
                translate([-4, 1.8, 3.8]) sphere(r=0.6);
                
                // Wing Tip 
                translate([-5, 17.5, 5.0]) sphere(r=0.2);
                translate([-7, 17.5, 5.0]) sphere(r=0.2);
            }
        }
    }

    // Tail Fin
    color(c_wing)
    hull() {
        translate([-11, 0, 6]) sphere(r=0.4);
        translate([-16, 0, 6]) sphere(r=0.4);
        translate([-15, 0, 11.8]) sphere(r=0.2); 
        translate([-13, 0, 11.8]) sphere(r=0.2);
    }

    // elevator
    color(c_wing)
    for (i = [-1, 1]) {
        scale([1, i, 1]) {
            hull() {
                translate([-14, 0.5, 5.5]) sphere(r=0.4);
                translate([-17, 0.5, 5.5]) sphere(r=0.4);
                translate([-16, 6.5, 5.5]) sphere(r=0.15);
                translate([-18, 6.5, 5.5]) sphere(r=0.15);
            }
        }
    }

    // eNGINES
    color(c_engine)
    for (i = [-1, 1]) {
        translate([2, i * 6, 2.8]) {
            // Main Engine Casing
            rotate([0, 90, 0]) cylinder(h=4.5, r=1.1, center=true);
            // Tapered Exhaust Cone
            translate([-2.25, 0, 0]) rotate([0, -90, 0]) cylinder(h=1, r1=1.1, r2=0.5);
        }
    }

    // Nose Gear
    translate([12.5, 0, 0]) {
        // strut
        color(c_strut) translate([0, 0, 2]) cylinder(h=3, r=0.15, center=true);
        // wheel
        color(c_tire) translate([0, 0, 0.4]) rotate([90, 0, 0]) cylinder(h=0.6, r=0.4, center=true);
    }

    // Main Gear
    for (i = [-1, 1]) {
        translate([-3, i * 3.5, 0]) {
            // strut
            color(c_strut) translate([0, 0, 2]) cylinder(h=3.5, r=0.2, center=true);
            // wheel
            color(c_tire) translate([0, 0.4, 0.6]) rotate([90, 0, 0]) cylinder(h=0.4, r=0.6, center=true);
            color(c_tire) translate([0, -0.4, 0.6]) rotate([90, 0, 0]) cylinder(h=0.4, r=0.6, center=true);
        }
    }
}



airliner();