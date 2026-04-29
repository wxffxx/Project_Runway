// Ground Power Unit (GPU) Cart
// Low-poly, architectural toy style

$fn = 16;

module GPU_Cart() {
    // trailer
    color("dimgray")
    translate([-0.3, 0, 0.3])
        cube([2.6, 1.3, 0.1], center=true);

    // enclosure
    color("white")
    translate([-0.3, 0, 0.85])
        cube([2.4, 1.2, 1.0], center=true);


    // grille
    color("silver")
    translate([0.3, -0.61, 0.85])
        cube([1.0, 0.1, 0.6], center=true);

    // control panel
    color("darkslategray")
    translate([-0.3, 0.61, 0.85])
        cube([0.8, 0.1, 0.5], center=true);

    // Exhaust pipe
    color("silver")
    translate([-1.0, 0, 1.35])
        cylinder(h=0.15, r=0.15, center=false);

    //Tow Bar Assembly
    color("orange")
    translate([1.5, 0, 0.25])
        cube([1.0, 0.15, 0.1], center=true);

    // Tow ring
    color("orange")
    translate([2.0, 0, 0.25])
        difference() {
            cylinder(h=0.1, r=0.15, center=true);
            cylinder(h=0.12, r=0.08, center=true);
        }

    //Wheels
    wheel_x_positions = [-1.1, 0.6];
    wheel_y_positions = [-0.65, 0.65];

    // Tires
    color("black") {
        for (x = wheel_x_positions) {
            for (y = wheel_y_positions) {
                translate([x, y, 0.25])
                rotate([90, 0, 0])
                    cylinder(h=0.2, r=0.25, center=true);
            }
        }
    }
    
    // hubcaps
    color("silver") {
        for (x = wheel_x_positions) {
            for (y = wheel_y_positions) {
                y_offset = (y > 0) ? y + 0.05 : y - 0.05;
                translate([x, y_offset, 0.25])
                rotate([90, 0, 0])
                    cylinder(h=0.15, r=0.12, center=true);
            }
        }
    }
}


GPU_Cart();