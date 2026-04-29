// Low-Poly Civilian Transport Helicopter

$fn = 16;

module civilian_helicopter() {
    // Cabin
    translate([1, 0, 1.8])
        scale([3, 1.5, 1.5])
            sphere(r=1);

    // Tail
    translate([-1.5, 0, 1.8])
        rotate([0, -90, 0])
            cylinder(h=4.5, r1=0.6, r2=0.2);

    translate([-6, 0, 1.8]) {
        // Vertical Tail Fin
        translate([-0.3, -0.1, -0.6])
            cube([0.6, 0.2, 1.6]);
    }

    // Rotor
    translate([1, 0, 3.3])
        cylinder(h=0.6, r=0.3);

    translate([1, 0, 3.9]) {
        rotate([0, 90, 0])
            cylinder(h=10, r=0.05, center=true);
        rotate([90, 0, 0])
            cylinder(h=10, r=0.05, center=true);
    }

    // Skids 
    // Left
    translate([1, 1.5, 0.1])
        rotate([0, 90, 0])
            cylinder(h=6, r=0.1, center=true);
    // Right
    translate([1, -1.5, 0.1])
        rotate([0, 90, 0])
            cylinder(h=6, r=0.1, center=true);

    // Skid Struts
    translate([2.5, 1.5, 0.1]) cylinder(h=1.2, r=0.08);
    translate([-0.5, 1.5, 0.1]) cylinder(h=1.2, r=0.08);
    
    translate([2.5, -1.5, 0.1]) cylinder(h=1.2, r=0.08);
    translate([-0.5, -1.5, 0.1]) cylinder(h=1.2, r=0.08);
}

civilian_helicopter();