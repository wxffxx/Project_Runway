
$fn = 16;

module follow_me_car() {
    color("darkgray") {
        for (x = [-1.2, 1.2]) {
            for (y = [-0.8, 0.8]) {
                translate([x, y, 0.4]) 
                    rotate([90, 0, 0]) 
                    cylinder(h=0.4, r=0.4, center=true);
            }
        }
    }

    // body
    color("yellow") {
        translate([0, 0, 0.65]) 
            cube([4, 1.6, 0.5], center=true);
            
        // upper cabin
        translate([-0.2, 0, 1.3]) 
            cube([2.2, 1.4, 0.8], center=true);
    }

    // light bar
    color("red") {
        translate([-0.2, 0, 1.75]) 
            cube([0.5, 1.0, 0.1], center=true);
    }
}

follow_me_car();