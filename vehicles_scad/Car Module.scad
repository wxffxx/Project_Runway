// Car Module

module simple_car() {
    body_length = 4.5;
    body_width = 1.8;
    lower_body_height = 0.6;
    body_clearance = 0.2; 
    
    cabin_length = 2.4;
    cabin_width = 1.6;
    cabin_height = 0.6;
    cabin_offset_x = -0.3;
    
    wheel_radius = 0.35;
    wheel_thickness = 0.4;
    wheel_base_x = 2.6; 
    wheel_track_y = 1.6; 


    // Lower Chassis
    color("DodgerBlue")
    translate([0, 0, body_clearance + (lower_body_height / 2)])
        cube([body_length, body_width, lower_body_height], center=true);

    // Upper Cabin
    color("LightSkyBlue")
    translate([cabin_offset_x, 0, body_clearance + lower_body_height + (cabin_height / 2)])
        cube([cabin_length, cabin_width, cabin_height], center=true);

    // wheels
    module wheel() {
        color("DarkSlateGray")
        rotate([90, 0, 0])
        cylinder(h=wheel_thickness, r=wheel_radius, center=true, $fn=16);
    }

    // Front Left
    translate([wheel_base_x / 2, wheel_track_y / 2, wheel_radius]) 
        wheel();
    
    // Front Right
    translate([wheel_base_x / 2, -wheel_track_y / 2, wheel_radius]) 
        wheel();
    
    // Rear Left
    translate([-wheel_base_x / 2, wheel_track_y / 2, wheel_radius]) 
        wheel();
    
    // Rear Right
    translate([-wheel_base_x / 2, -wheel_track_y / 2, wheel_radius]) 
        wheel();
}

simple_car();