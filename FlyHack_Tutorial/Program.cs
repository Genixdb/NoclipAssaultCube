using Swed32;
using System.Numerics;
using System.Runtime.InteropServices;

// memory handler
Swed swed = new Swed("ac_client");

// addresses
IntPtr moduleBase = swed.GetModuleBase("ac_client.exe");
IntPtr playerPositionAddress = swed.ReadPointer(moduleBase, 0x0017E360) + 0x28;
IntPtr viewAnglesAddress = swed.ReadPointer(moduleBase, 0x0017E360) + 0x38;
IntPtr moveInstructionAddress = moduleBase + 0xC0AA3;

// variables
Vector3 currentPosition;
Vector3 viewAngles;
float moveSpeed = 0.3f;
double degreeToRadian = Math.PI / 180;
double anglesOffset = 90 * degreeToRadian;

bool noclipEnabled = false;
bool lastF1State = false;

// main loop
while (true)
{
    bool currentF1State = (GetAsyncKeyState(0x70) & 0x8000) != 0; // F1 key

    // Detect rising edge (key just pressed)
    if (currentF1State && !lastF1State)
    {
        noclipEnabled = !noclipEnabled; // toggle
        Console.Clear();
        Console.ForegroundColor = noclipEnabled ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(noclipEnabled ? "Noclip enabled" : "Noclip disabled");
        Thread.Sleep(150); // debounce
    }

    lastF1State = currentF1State;

    if (noclipEnabled)
    {
        swed.WriteBytes(moveInstructionAddress, "90 90"); // disable fall
        Noclip();
    }
    else
    {
        swed.WriteBytes(moveInstructionAddress, "84 DB"); // enable fall
    }

    Thread.Sleep(10);
}

void Noclip()
{
    bool wKey = GetAsyncKeyState(0x57) < 0; // W
    bool aKey = GetAsyncKeyState(0x41) < 0; // A
    bool sKey = GetAsyncKeyState(0x53) < 0; // S
    bool dKey = GetAsyncKeyState(0x44) < 0; // D
    bool jKey = GetAsyncKeyState(0x20) < 0; // Space
    bool cKey = GetAsyncKeyState(0x11) < 0; // Ctrl

    viewAngles.X = swed.ReadFloat(viewAnglesAddress);
    viewAngles.Y = swed.ReadFloat(viewAnglesAddress + 0x4);
    currentPosition = swed.ReadVec(playerPositionAddress);

    float newX = currentPosition.X;
    float newY = currentPosition.Y;
    float newZ = currentPosition.Z;

    float forwardX = (float)(moveSpeed * Math.Cos(viewAngles.X * degreeToRadian + anglesOffset));
    float forwardY = (float)(moveSpeed * Math.Sin(viewAngles.X * degreeToRadian + anglesOffset));
    float forwardZ = (float)(moveSpeed * Math.Sin(viewAngles.Y * degreeToRadian));

    float rightX = (float)(moveSpeed * Math.Sin(viewAngles.X * degreeToRadian + anglesOffset));
    float rightY = (float)(moveSpeed * Math.Cos(viewAngles.X * degreeToRadian + anglesOffset));

    if (wKey) { newX += forwardX; newY += forwardY; newZ += forwardZ; }
    if (sKey) { newX -= forwardX; newY -= forwardY; newZ -= forwardZ; }
    if (aKey) { newX += rightX; newY -= rightY; }
    if (dKey) { newX -= rightX; newY += rightY; }
    if (jKey) { newZ += moveSpeed; }
    if (cKey) { newZ -= moveSpeed; }

    swed.WriteVec(playerPositionAddress, new Vector3(newX, newY, newZ));
}


// import GetAsyncKeyState
[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey);
