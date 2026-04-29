// Airport Pushback Tractor

module pushback_tractor() {

    body_length = 7.0;
    body_width = 3.2;
    body_height = 1.0;
    ground_clearance = 0.5;
    
    cab_length = 2.0;
    cab_width = 2.4;
    cab_height = 1.0;
    
    wheel_radius = 0.75;
    wheel_width = 0.8;
    wheelbase_x = 4.5;
    track_y = 3.2; 

    union() {
        color("Gold") 
        translate([0, 0, ground_clearance + (body_height / 2)])
            cube([body_length, body_width, body_height], center=true);

        color("DarkSlateGray") 
        translate([1.0, 0, ground_clearance + body_height + (cab_height / 2)])
            cube([cab_length, cab_width, cab_height], center=true);

        //Wheels
        color("#222222") {
            // Left
            translate([wheelbase_x / 2, track_y / 2, wheel_radius])
                rotate([90, 0, 0])
                cylinder(h=wheel_width, r=wheel_radius, center=true, $fn=16);

            // Right
            translate([wheelbase_x / 2, -track_y / 2, wheel_radius])
                rotate([90, 0, 0])
                cylinder(h=wheel_width, r=wheel_radius, center=true, $fn=16);

            // Rear Left
            translate([-wheelbase_x / 2, track_y / 2, wheel_radius])
                rotate([90, 0, 0])
                cylinder(h=wheel_width, r=wheel_radius, center=true, $fn=16);

            // Rear Right
            translate([-wheelbase_x / 2, -track_y / 2, wheel_radius])
                rotate([90, 0, 0])
                cylinder(h=wheel_width, r=wheel_radius, center=true, $fn=16);
        }

        color("DimGray") {
            // Front Hitch
            translate([(body_length / 2) + 0.15, 0, ground_clearance + 0.25])
                cube([0.3, 0.8, 0.5], center=true);
                
            // Rear Hitch
            translate([-(body_length / 2) - 0.15, 0, ground_clearance + 0.25])
                cube([0.3, 0.8, 0.5], center=true);
        }
    }
}

pushback_tractor();