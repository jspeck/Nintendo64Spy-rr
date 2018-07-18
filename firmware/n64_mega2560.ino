//	Arduino Mega 2560

#define SERIAL_BUFFER_SIZE 256

#define PIN_READ( pin )  (PINE&(1<<(pin)))
#define PINC_READ( pin ) (PINC&(1<<(pin)))
#define MICROSECOND_NOPS "nop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\nnop\n"

#define WAIT_FALLING_EDGE( pin ) while( !PIN_READ(pin) ); while( PIN_READ(pin) );

#define MODEPIN_N64  1

#define N64_PIN        4
#define N64_PREFIX     9
#define N64_BITCOUNT  32

#define ZERO  '\0'  // Use a byte value of 0x00 to represent a bit with value 0.
#define ONE    '1'  // Use an ASCII one to represent a bit with value 1.  This makes Arduino debugging easier.
#define SPLIT '\n'  // Use a new-line character to split up the controller state packets.




// Declare some space to store the bits we read from a controller.
unsigned char rawData[ 256 ];

// 8 bytes of data that we get from the controller
struct state{
    char stick_x;
    char stick_y;
    // bits: 0, 0, 0, start, y, x, b, a
    unsigned char data1;
    // bits: 1, L, R, Z, Dup, Ddown, Dright, Dleft
    unsigned char data2;
} N64_status;


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// General initialization, just sets all pins to input and starts serial communication.
void setup()
{
    PORTE = 0x00;
    DDRD  = 0x00;
    PORTD = 0xFF; // Set the pull-ups on the port we use to check operation mode.
    DDRC  = 0x00;
    Serial.begin( 115200 );
}

void translate_raw_data()
{
    // The get_N64_status function sloppily dumps its data 1 bit per byte
    // into the get_status_extended char array. It's our job to go through
    // that and put each piece neatly into the struct N64_status
    memset(&N64_status, 0, sizeof(N64_status));
    // line 1
    // bits: A, B, Z, Start, Dup, Ddown, Dleft, Dright
    // line 2
    // bits: 0, 0, L, R, Cup, Cdown, Cleft, Cright
    // line 3
    // bits: joystick x value
    // These are 8 bit values centered at 0x80 (128)
    // line 4
    // bits: joystick 4 value
    // These are 8 bit values centered at 0x80 (128)
    for (int i=0; i<8; i++) {
        N64_status.data1 |= rawData[i+9] ? (0x80 >> i) : 0;
        N64_status.data2 |= rawData[8+i+9] ? (0x80 >> i) : 0;
        N64_status.stick_x |= rawData[16+i+9] ? (0x80 >> i) : 0;
        N64_status.stick_y |= rawData[24+i+9] ? (0x80 >> i) : 0;
    }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Performs a read cycle from one of Nintendo's one-wire interface based controllers.
// This includes the N64 and the Gamecube.
//     pin  = Pin index on Port D where the data wire is attached.
//     bits = Number of bits to read from the line.
template< unsigned char pin >
void read_oneWire( unsigned char bits )
{
    unsigned char *rawDataPtr = rawData;

read_loop:

    // Wait for the line to go high then low.
    WAIT_FALLING_EDGE( pin );

    // Wait ~2us between line reads
    asm volatile( MICROSECOND_NOPS MICROSECOND_NOPS );

    // Read a bit from the line and store as a byte in "rawData"
    *rawDataPtr = PIN_READ(pin);
    ++rawDataPtr;
    if( --bits == 0 ) return;

    goto read_loop;
}

// Verifies that the 9 bits prefixing N64 controller data in 'rawData'
// are actually indicative of a controller state signal.
inline bool checkPrefixN64 ()
{
    if( rawData[0] != 0 ) return false; // 0
    if( rawData[1] != 0 ) return false; // 0
    if( rawData[2] != 0 ) return false; // 0
    if( rawData[3] != 0 ) return false; // 0
    if( rawData[4] != 0 ) return false; // 0
    if( rawData[5] != 0 ) return false; // 0
    if( rawData[6] != 0 ) return false; // 0
    if( rawData[7] == 0 ) return false; // 1
    if( rawData[8] == 0 ) return false; // 1
    return true;
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Sends a packet of controller data over the Arduino serial interface.
inline void sendRawData( unsigned char first, unsigned char count )
{
    for( unsigned char i = first ; i < first + count ; i++ ) {
        Serial.write( rawData[i] ? ONE : ZERO );
    }
    Serial.write( SPLIT );
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Arduino sketch main loop definition.
void loop()
{
    noInterrupts();
    read_oneWire< N64_PIN >( N64_PREFIX + N64_BITCOUNT );
    interrupts();
    if( checkPrefixN64() ) {
        translate_raw_data();
        Serial.write((uint8_t *)&N64_status,4);
        delay(10);
    }
}
