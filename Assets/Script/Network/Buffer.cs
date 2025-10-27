using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// �����͸� ��� ����, ����
public class Buffer
{
    
    struct Packet
    {
        public int pos; // �ش� ��Ŷ�� stream ��� ��ġ�� �����ϴ��� ���
        public int size; // ũ�� ���
    };

    // ���� ������ �����
    MemoryStream stream;
    // ��Ŷ���� ������� ��� ����Ʈ
    List<Packet> list;
    int pos = 0; // MemoryStream �� ���� ���� ��ġ

    Object o = new System.Object(); // lock�� ����ȭ Ű
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

        // ��Ŷ ���� �۾� �� �ٸ� �������� ���� ����
        // �ٸ� ����� �߰��� �����͸� �����ع��� �� ������
        // �� lock�� �ʹ� ������ ������ ���� �� ������ ���� �ʿ�
        lock (o)
        {
            list.Add(packet);

            // ��Ʈ���� ���� ��ġ = Buffer�� pos��ġ
            stream.Position = pos;
            // ���ۿ� �����͸� ����ص�
            stream.Write(bytes, 0, length);
            // �޸� ��Ʈ���� ���� ���۸� ��� �����Ͱ� Ȯ���� ����ǵ��� ��
            // Flush() - MemoryStream������ ���ʿ������� �������� ���� ȣ��
            //stream.Flush();
            // ���� ���۹����� ����
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
            // ���� �� �ִ� ��ŭ�� �б�
            while (readBytes < length && list.Count > 0)
            {
                Packet packet = list[0]; // �� ���� ������ ó��
                // stream : �Ŵ��� �ܼ�(�迭) + Ŀ����� �����ϸ� �����ϱ� ����
                stream.Position = packet.pos;

                // ������ ����������� �����͸� �е��� Min���� ���
                int bytesToRead = Math.Min(length - readBytes, packet.size);
                stream.Read(bytes, readBytes, bytesToRead);
                readBytes += bytesToRead;

                // �� �������� ������ �ʵ��� ���
                if (bytesToRead == packet.size)
                {
                    // �� ���� ��Ŷ ����
                    list.RemoveAt(0);
                }
                else
                {
                    packet.pos += bytesToRead;
                    packet.size -= bytesToRead;
                    list[0] = packet;
                }
            }

            // �޸� ����ȭ
            if (list.Count == 0)
            {
                stream.SetLength(0);
                pos = 0;
            }
        }

        return readBytes;
    }
}