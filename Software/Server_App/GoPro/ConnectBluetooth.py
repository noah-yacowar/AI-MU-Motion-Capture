import asyncio
from open_gopro import WirelessGoPro
from open_gopro.features.streaming.livestream import LivestreamOptions
from open_gopro.features.streaming.base_stream import StreamType
from open_gopro.models.proto import EnumWindowSize, EnumLens, EnumLiveStreamStatus
from returns.pipeline import is_successful
from open_gopro.models.constants import Toggle

async def connect_to_wifi(camera: WirelessGoPro, ssid: str, password: str) -> bool:
    scan_result = await camera.access_point.scan_wifi_networks()
    if not is_successful(scan_result):
        print("❌ Failed to scan for Wi-Fi networks.")
        return False

    for entry in scan_result.unwrap().entries:
        if entry.ssid == ssid:
            print(f"✅ Found SSID '{ssid}', attempting to connect...")
            result = await camera.access_point.connect(ssid=ssid, password=password)
            if is_successful(result):
                print(f"✅ Connected GoPro to {ssid}!")
                return True
            else:
                print(f"❌ Failed to connect to Wi-Fi: {result.failure()}")
                return False

    print(f"❌ SSID '{ssid}' not found in scan results.")
    return False

async def wait_until_livestream_ready(camera: WirelessGoPro, timeout=15):
    print("🔄 Waiting for livestream to become ready...")
    for _ in range(timeout):
        status_result = await camera.ble_command.get_livestream_status()
        if status_result.is_success:
            status = status_result.unwrap()
            stream_status = status.live_stream_status
            print(f"📶 Status: {stream_status}")
            if stream_status == EnumLiveStreamStatus.LIVE_STREAM_STATE_READY:
                print("✅ Livestream is ready!")
                return True
        await asyncio.sleep(1)
    print("❌ Livestream did not become ready in time.")
    return False

async def main():
    ssid = "CVISS_5G"
    password = "snarasim"
    rtmp_url = "rtmp://192.168.1.132/live/stream1"

    camera = WirelessGoPro(interfaces={WirelessGoPro.Interface.BLE})
    await camera.open()

    connected = await connect_to_wifi(camera, ssid, password)
    if not connected:
        await camera.close()
        return
    
    await asyncio.sleep(10) 

    options = LivestreamOptions(
        url=rtmp_url,
    )

    result = await camera.streaming.start_stream(StreamType.LIVE, options)

    await wait_until_livestream_ready(camera)
    
    await camera.control.set_shutter(shutter=Toggle.ENABLE)


    if is_successful(result):
        print("RTMP livestream started!")
        await asyncio.sleep(60)
        await camera.streaming.stop_active_stream()
    else:
        print(f"❌ Failed to start stream: {result.failure()}")

    await camera.close()

asyncio.run(main())

