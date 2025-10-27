using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// 데이터를 담는 공간, 문간
public class Buffer
{
    
    struct Packet
    {
        public int pos; // 해당 패킷이 stream 어느 위치에 존재하는지 기록
        public int size; // 크기 기록
    };

    // 실제 데이터 저장소
    MemoryStream stream;
    // 패킷들을 순서대로 담는 리스트
    List<Packet> list;
    int pos = 0; // MemoryStream 내 현재 쓰기 위치

    Object o = new System.Object(); // lock용 동기화 키
    public Buffer()
    {
        stream = new MemoryStream();
        list = new List<Packet>();
    }

    public int Push(byte[] bytes, int length)
    {
        if (bytes == null || length <= 0 || length > bytes.Length) return 0;
        Packet packet = new Packet();

        packet.pos = pos;
        packet.size = length;

        // 패킷 쓰기 작업 중 다른 스레드의 접근 방지
        // 다른 사람이 중간에 데이터를 수정해버릴 수 없도록
        // 단 lock이 너무 많으면 문제가 생길 수 있으니 주의 필요
        lock (o)
        {
            list.Add(packet);

            // 스트림의 현재 위치 = Buffer의 pos위치
            stream.Position = pos;
            // 버퍼에 데이터를 기록해둠
            stream.Write(bytes, 0, length);
            // 메모리 스트림의 내부 버퍼를 비워 데이터가 확실히 저장되도록 함
            // Flush() - MemoryStream에서는 불필요하지만 안전성을 위해 호출
            //stream.Flush();
            // 다음 시작번지를 조정
            pos += length;
        }

        return length;
    }

    public int Pop(ref byte[] bytes, int length)
    {
        if (bytes == null || length <= 0) return 0;

        int readBytes = 0;
        lock (o)
        {
            // 읽을 수 있는 만큼만 읽기
            while (readBytes < length && list.Count > 0)
            {
                Packet packet = list[0]; // 맨 앞의 데이터 처리
                // stream : 거대한 콘솔(배열) + 커서라고 생각하면 이해하기 쉽다
                stream.Position = packet.pos;

                // 가능한 사이즈까지만 데이터를 읽도록 Min값을 기록
                int bytesToRead = Math.Min(length - readBytes, packet.size);
                stream.Read(bytes, readBytes, bytesToRead);
                readBytes += bytesToRead;

                // 다 못읽으면 지우지 않도록 방어
                if (bytesToRead == packet.size)
                {
                    // 맨 앞의 패킷 삭제
                    list.RemoveAt(0);
                }
                else
                {
                    packet.pos += bytesToRead;
                    packet.size -= bytesToRead;
                    list[0] = packet;
                }
            }

            // 메모리 최적화
            if (list.Count == 0)
            {
                stream.SetLength(0);
                pos = 0;
            }
        }

        return readBytes;
    }
}