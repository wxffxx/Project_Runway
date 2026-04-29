// Passenger Shuttle Bus

module airport_shuttle() {
    L = 14.0;
    W = 3.0;
    H = 3.5;

    // proportions
    clearance = 0.4;
    ac_h = 0.3;
    body_h = H - clearance - ac_h; // 2.8
    wheel_r = 0.5;
    wheel_w = 0.6;
    e = 0.02;

    // wheels
    color("darkgray") {
        axle_offset = L/2 - 2.5;
        for (x = [axle_offset, -axle_offset]) {
            for (y = [W/2, -W/2]) {
                translate([x, y, wheel_r])
                rotate([90, 0, 0])
                cylinder(h=wheel_w, r=wheel_r, center=true, $fn=32);
            }
        }
    }

    //MAIN
    translate([0, 0, clearance]) {
        // White chassis/body
        color("whitesmoke")
        translate([0, 0, body_h/2])
        cube([L, W, body_h], center=true);

        window_h = 1.2;
        window_z = body_h - window_h/2 - 0.3;

        color("darkslategray") {
            // window bands
            for (y = [W/2, -W/2]) {
                translate([0, y + (y>0 ? e : -e), window_z])
                cube([L - 1, 0.1, window_h], center=true);
            }

            // windshield
            translate([L/2 + e, 0, window_z - 0.2])
            cube([0.1, W - 0.4, window_h + 0.4], center=true);

            // rear window
            translate([-L/2 - e, 0, window_z])
            cube([0.1, W - 0.4, window_h], center=true);
        }

        //doors
        door_w = 1.8;
        door_h = 2.3;
        door_z = door_h/2 + 0.1; // Slight step up from bottom

        color("slategray") {
            for (y = [W/2, -W/2]) {
                // 3 sets of doors evenly spaced per side
                for (x = [-3.5, 0, 3.5]) {
                    translate([x, y + (y>0 ? e*2 : -e*2), door_z])
                    cube([door_w, 0.15, door_h], center=true);
                }
            }
        }
    }

    //ac
    color("silver")
    translate([0, 0, clearance + body_h + ac_h/2])
    cube([L/2, W/1.5, ac_h], center=true);
}

airport_shuttle();