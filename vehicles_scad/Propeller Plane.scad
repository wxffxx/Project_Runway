// Single Engine Propeller Plane

$fn = 16;

module simple_plane() {
    color("White")
    translate([0, 0, 1.5])
    rotate([0, 90, 0])
    cylinder(h=7, r=0.8, center=true);

    //Spinner
    color("Red")
    translate([3.5, 0, 1.5])
    sphere(r=0.8);

    //Cockpit
    color("SkyBlue")
    translate([0.5, 0, 2.1])
    cube([1.8, 1.2, 0.8], center=true);

    //wings
    color("White")
    translate([1, 0, 1.5])
    cube([1.6, 11, 0.2], center=true);


    // Horiz Stab
    color("White")
    translate([-3.2, 0, 1.5])
    cube([1, 3, 0.2], center=true);

    // Vert Stab
    color("White")
    translate([-3.2, 0, 2.1])
    cube([1, 0.2, 1.2], center=true);

    // Propeller
    color("DarkGray")
    translate([4.3, 0, 1.5])
    cube([0.1, 0.3, 2.6], center=true); 
    
    // Main Wheels
    color("Black")
    translate([1.2, 1.5, 0.5])
    rotate([90, 0, 0])
    cylinder(h=0.4, r=0.5, center=true);

    color("Black")
    translate([1.2, -1.5, 0.5])
    rotate([90, 0, 0])
    cylinder(h=0.4, r=0.5, center=true);
    
    // Struts
    color("DarkGray")
    translate([1.2, 1.3, 1.0])
    cylinder(h=1.0, r=0.1, center=true);

    color("DarkGray")
    translate([1.2, -1.3, 1.0])
    cylinder(h=1.0, r=0.1, center=true);

    // Tail Wheel
    color("Black")
    translate([-3.2, 0, 0.25])
    rotate([90, 0, 0])
    cylinder(h=0.2, r=0.25, center=true, $fn=12);
    
    // Strut
    color("DarkGray")
    translate([-3.2, 0, 0.5])
    cylinder(h=0.5, r=0.08, center=true, $fn=8);
}




simple_plane();