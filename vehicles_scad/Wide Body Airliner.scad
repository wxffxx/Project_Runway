module WideBodyAirliner() {
    // Strict low-poly architectural scale
    $fn = 16;
    
    // FUSELAGE
    color("white")
    hull() {
        // Nose
        translate([28, 0, 8.5]) sphere(r=2);
        // cockpit
        translate([24, 0, 8.5]) sphere(r=3.2);
        // Center wing
        translate([5, 0, 8.5]) sphere(r=3.5);
        // body taper
        translate([-15, 0, 9]) sphere(r=3.2);
        // tail cone
        translate([-33, 0, 10]) sphere(r=1);
    }

    color("silver") {
        // Right
        hull() {
            translate([8, 3, 7]) sphere(r=1.2);
            translate([-2, 3, 7]) sphere(r=1.2);
            translate([-12, 29.5, 8]) sphere(r=0.4);
            translate([-18, 29.5, 8]) sphere(r=0.4);
        }
        // Left
        hull() {
            translate([8, -3, 7]) sphere(r=1.2);
            translate([-2, -3, 7]) sphere(r=1.2);
            translate([-12, -29.5, 8]) sphere(r=0.4);
            translate([-18, -29.5, 8]) sphere(r=0.4);
        }
    }

  
    // ENGINES
    color("darkgray") {
        // Right
        translate([2, 10, 4.5]) rotate([0, 90, 0]) cylinder(r=2.2, h=8, center=true);
        // Pylon
        hull() {
            translate([0, 10, 6.5]) sphere(r=0.5);
            translate([-2, 10, 7]) sphere(r=0.5);
            translate([0, 10, 4.5]) sphere(r=0.5);
        }
        
        // Left
        translate([2, -10, 4.5]) rotate([0, 90, 0]) cylinder(r=2.2, h=8, center=true);
        // Pylon
        hull() {
            translate([0, -10, 6.5]) sphere(r=0.5);
            translate([-2, -10, 7]) sphere(r=0.5);
            translate([0, -10, 4.5]) sphere(r=0.5);
        }
    }


    // Vert
    color("silver")
    hull() {
        translate([-25, 0, 10]) sphere(r=1);
        translate([-33, 0, 10]) sphere(r=0.5);
        translate([-34, 0, 17.5]) sphere(r=0.5);
        translate([-29, 0, 17.5]) sphere(r=0.5);
    }

    // Horiz
    color("silver") {
        // Right
        hull() {
            translate([-28, 2, 10.5]) sphere(r=0.8);
            translate([-33, 2, 10.5]) sphere(r=0.5);
            translate([-35, 12, 10.5]) sphere(r=0.3);
            translate([-32, 12, 10.5]) sphere(r=0.3);
        }
        // Left
        hull() {
            translate([-28, -2, 10.5]) sphere(r=0.8);
            translate([-33, -2, 10.5]) sphere(r=0.5);
            translate([-35, -12, 10.5]) sphere(r=0.3);
            translate([-32, -12, 10.5]) sphere(r=0.3);
        }
    }

    // GEAR 
    color("black") {
        // Nose
        translate([22, 0, 1]) rotate([90, 0, 0]) cylinder(r=1, h=1.5, center=true);
        translate([22, 0, 2]) cylinder(r=0.3, h=6); // Strut to body
        
        // Right
        translate([-2, 9, 1.5]) rotate([90, 0, 0]) cylinder(r=1.5, h=2.5, center=true);
        translate([-2, 9, 2.5]) cylinder(r=0.6, h=5); // Strut to wing root
        
        // Left
        translate([-2, -9, 1.5]) rotate([90, 0, 0]) cylinder(r=1.5, h=2.5, center=true);
        translate([-2, -9, 2.5]) cylinder(r=0.6, h=5); // Strut to wing root
    }
}

WideBodyAirliner();