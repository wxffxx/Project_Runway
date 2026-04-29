
$fn = 16;

module airport_catering_truck() {
    
    color("black") {
        // Front
        translate([3, 0, 0.6]) 
            rotate([90, 0, 0]) 
            cylinder(h=3.2, r=0.6, center=true);
            
        // Rear
        translate([-3, 0, 0.6]) 
            rotate([90, 0, 0]) 
            cylinder(h=3.2, r=0.6, center=true);
    }

    color("darkgray") {
        translate([0, 0, 1.0]) 
            cube([9, 2.8, 0.8], center=true);
    }

    // Cab
    color("lightgray") {
        translate([3.25, 0, 2.4]) 
            cube([2.5, 2.8, 2.0], center=true);
    }

    // Scissor Lift
    color("gray") {
        translate([-1.5, 0, 2.45]) {
            rotate([0, 22.8, 0]) 
                cube([5.42, 2.0, 0.3], center=true);
            rotate([0, -22.8, 0]) 
                cube([5.42, 2.0, 0.3], center=true);
        }
    }

    // Catering Box
    color("white") {
        translate([-1.5, 0, 4.75]) 
            cube([6.5, 3.0, 2.5], center=true);
    }
}

airport_catering_truck();