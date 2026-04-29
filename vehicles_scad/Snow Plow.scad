$fn = 16;

module airport_snow_plow() {
   
    translate([-0.5, 0, 0]) {
        
        //main
        color("orange") {
            translate([-0.25, 0, 1])
                cube([8.5, 2.5, 0.5], center=true);

            // cab
            translate([1.5, 0, 2.375])
                cube([2.5, 3, 2.25], center=true);

            translate([-2.25, 0, 2])
                cube([4, 2.5, 1.5], center=true);
        }
        
        // Windshield
        color("lightskyblue") {
            translate([2.76, 0, 2.5])
                rotate([0, 5, 0])
                cube([0.1, 2.8, 1.2], center=true);
                
            // Side Windows
            translate([1.5, 0, 2.5])
                cube([2.55, 3.02, 1], center=true);
        }

        // PLOW BLADE
        color("goldenrod") {
            translate([5.25, 0, 0.75])
            rotate([0, 0, -20]) // Angled to cast snow to the side
            difference() {
                cube([0.5, 4.2, 1.5], center=true);
                
                translate([1, 0, 0.75])
                    rotate([90, 0, 0])
                    cylinder(h=4.5, r=1.2, center=true);
            }
            
            color("darkgray")
            translate([4.4, 0, 0.8])
                cube([1.5, 1, 0.5], center=true);
        }

        color("goldenrod") {
            // Cylindrical brush mechanism suspended centrally
            translate([-0.5, 0, 0.55])
                rotate([0, 90, -15]) // Angled horizontally like a runway sweeper

                
            color("darkgray")
            translate([-0.5, 0, 1.1])
                cube([1.5, 3.8, 0.2], center=true);
        }

        // wheels
        color("darkslategray") {
            // Front Right
            translate([2.5, -1.5, 0.75])
                rotate([90, 0, 0])
                cylinder(h=1, r=0.75, center=true);
                
            // Front Left
            translate([2.5, 1.5, 0.75])
                rotate([90, 0, 0])
                cylinder(h=1, r=0.75, center=true);
                
            // Rear Right
            translate([-3.25, -1.5, 0.75])
                rotate([90, 0, 0])
                cylinder(h=1, r=0.75, center=true);
                
            // Rear Left
            translate([-3.25, 1.5, 0.75])
                rotate([90, 0, 0])
                cylinder(h=1, r=0.75, center=true);
        }
    }
}

airport_snow_plow();