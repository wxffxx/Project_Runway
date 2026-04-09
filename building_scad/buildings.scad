$fn = 16; 

// ATC Tower
module atc_tower() {
    // Main shaft
    color("darkgray") 
    union() {
        cylinder(h=40, r1=3, r2=2.5);
        translate([2, -1, 0]) cube([2, 2, 38]); //outer shaft
    }
    
    // Observation Deck Base
    color("gray")
    translate([0, 0, 38]) cylinder(h=3, r1=2.5, r2=4.5);
    
    // observation Deck
    color("lightblue")
    translate([0, 0, 41]) cylinder(h=3, r=4.5);
    
    // Roof Radome
    color("darkgray")
    translate([0, 0, 44]) cylinder(h=1, r=4.5);
    color("white")
    translate([0, 0, 45.5]) sphere(r=1.5); // Radome
    color("black")
    translate([2, 0, 45]) cylinder(h=4, r=0.1); // Antenna
}

// Maintenance Hangar
module hangar() {
    width = 30;
    depth = 20;
    height = 10;
    
    // Base structure with thicker walls
    color("slategray")
    difference() {
        cube([width, depth, height]);
        translate([2, -1, 0]) cube([width-4, depth+2, height-2]);
    }
    
    // Door tracks/frames
    color("darkgray")
    translate([1.5, -0.5, 0]) cube([1, 1, height-2]);
    translate([width-2.5, -0.5, 0]) cube([1, 1, height-2]);
    
    // arched roof
    color("gray")
    translate([width/2, 0, height])
    intersection() {
        rotate([-90, 0, 0]) cylinder(h=depth, r=width/2, $fn=32);
        // Bounding box to only keep the top half
        translate([-width/2, 0, 0]) cube([width, width/2, depth]);
    }
    
    // Roof HVAC/Vents
    color("silver")
    translate([width/2 - 2, depth/2, height + width/2 - 1]) cube([4, 4, 2]);
}

// Public Terminal
module public_terminal() {
    color("gray")
    cube([60, 15, 8]);
    
    // roof HVAC
    color("silver")
    for (x = [5 : 15 : 50]) {
        translate([x, 5, 8]) cube([4, 4, 1.5]);
    }
    
    // Entrance /ticketing
    color("darkgray")
    translate([15, -10, 0]) cube([30, 10, 6]);
    color("silver")
    translate([12, -12, 6]) cube([36, 6, 0.5]); 
    
    // bridge connection
    color("slategray")
    for (i = [10 : 20 : 50]) {
        translate([i, 15, 0]) cube([4, 5, 4]);
        translate([i+1, 20, 0]) cube([2, 4, 3]); 
    }
}
