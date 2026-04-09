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

// ILS, Lights, etc.

// light Mast
module light_mast() {
    color("darkgray") cylinder(h=15, r=0.3);
    color("silver") translate([-1, -0.5, 15]) cube([2, 1, 0.5]);
}

// ILS
module ils_array() {
    color("red")
    for(x = [0 : 2 : 20]) {
        translate([x, 0, 0]) cylinder(h=1.5, r=0.1);
        translate([x-0.5, -0.1, 1]) cube([1, 0.2, 0.5]);
    }
}

// PAPI
module papi_lights() {
    color("white")
    for(x = [0 : 2 : 6]) {
        translate([x, 0, 0]) cube([1, 1, 0.5]);
        translate([x+0.5, 0.5, -0.5]) cylinder(h=0.5, r=0.1);
    }
}

// fuel tank
module fuel_tank() {
    color("silver")
    cylinder(h=6, r=4);
    // safety berm
    color("gray")
    difference() {
        cylinder(h=1, r=6);
        translate([0,0,-0.1]) cylinder(h=1.2, r=5.5);
    }
}

// Surveillance Radar ASR
module radar_tower() {
    // base
    color("darkgray") cylinder(h=20, r=2);
    // turntable
    color("silver") translate([0, 0, 20]) cylinder(h=2, r=2.5);
    color("gray") translate([-3, -1, 22]) cube([6, 2, 4]);
}

// ARFF station 
module arff_station() {
    width = 35; depth = 20; height = 8;
    
    color("firebrick") cube([width, depth, height]);
    color("darkgray") {
        for (x = [5 : 10 : 25]) {
            translate([x, -0.5, 0]) cube([6, 1, 6]);
        }
    }
}

// VOR/DME Station
module vor_dme() {
    color("gray") cylinder(h=1, r=8);
    color("white") translate([0, 0, 1]) cylinder(h=4, r1=1.5, r2=0.5);
    color("white") translate([0, 0, 5]) sphere(r=0.5);
}

// Windsock
module windsock() {
    // visual circle
    color("white") difference() {
        cylinder(h=0.2, r=4);
        translate([0,0,-0.1]) cylinder(h=0.4, r=3.5);
    }
    //pole
    color("silver") cylinder(h=8, r=0.2);
    //sock
    color("orange") translate([0, 0, 7.5]) rotate([0, 90, 0]) cylinder(h=3, r1=0.6, r2=0.3);
}

// Cargo Terminal
module cargo_terminal() {
    // warehouse
    color("slategray") cube([60, 40, 12]);
    
    // Truck loading docks 
    color("darkgray") translate([5, -2, 0]) cube([50, 2, 4]);
    
    // cargo staging doors (airside)
    color("gray") {
        for(x = [10 : 15 : 40]) {
            translate([x, 39.5, 0]) cube([8, 1, 6]);
        }
    }
}

// Deicing
module deicing_facility() {
    // Drive through gantry
    color("goldenrod") {
        translate([0, 0, 0]) cube([2, 2, 12]); // Left pillar
        translate([20, 0, 0]) cube([2, 2, 12]); // Right pillar
        translate([0, 0, 12]) cube([22, 2, 2]); // Crossbeam
    }
    // Type I and Type IV fluid storage tanks
    color("darkgreen") {
        translate([-6, 5, 0]) cylinder(h=6, r=2.5);
        translate([-6, -2, 0]) cylinder(h=6, r=2.5);
    }
}


// Layout

translate([0, 0, 0]) public_terminal();
translate([80, 0, 0]) hangar();
translate([140, 0, 0]) cargo_terminal();

translate([0, 60, 0]) atc_tower();
translate([80, 60, 0]) arff_station();
translate([140, 60, 0]) deicing_facility();

translate([0, 120, 0]) radar_tower();
translate([80, 120, 0]) vor_dme();
translate([140, 120, 0]) windsock();

translate([0, 180, 0]) fuel_tank();
translate([30, 180, 0]) light_mast();
translate([50, 180, 0]) light_mast();
translate([80, 180, 0]) ils_array();
translate([120, 180, 1]) papi_lights();

