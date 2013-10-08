/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once

#ifndef __RTSP_TRACK_DEFINED
#define __RTSP_TRACK_DEFINED

#include "SimpleServer.h"
#include "RtspTransportResponseHeader.h"
#include "IpAddress.h"
#include "RtspTrackStatistics.h"
#include "PayloadType.h"
#include "RtpPacketCollection.h"

#define PORT_UNSPECIFIED                                              UINT_MAX
// receiver report minimum time is 5000 ms
#define RECEIVER_REPORT_MIN_TIME                                      5000

#define RTSP_TRACK_FLAG_NONE                                          0x00000000
#define RTSP_TRACK_FLAG_SENDER_SYNCHRONIZATION_SOURCE_IDENTIFIER_SET  0x00000001
#define RTSP_TRACK_FLAG_END_OF_STREAM                                 0x00000002

class CRtspTrack
{
public:
  // initializes a new instance of CRtspTrack class
  CRtspTrack(void);
  ~CRtspTrack(void);

  /* get methods */

  // gets server data port
  // @return : server data port or PORT_UNSPECIFIED if error
  unsigned int GetServerDataPort(void);

  // gets server control port
  // @return : server control port or PORT_UNSPECIFIED if error
  unsigned int GetServerControlPort(void);

  // gets client data port
  // @return : client data port or PORT_UNSPECIFIED if error
  unsigned int GetClientDataPort(void);

  // gets client control port
  // @return : client control port or PORT_UNSPECIFIED if error
  unsigned int GetClientControlPort(void);

  // gets track URL
  // @return : track URL or NULL if error
  const wchar_t *GetTrackUrl(void);

  // gets data server
  // @return : data server or NULL if not specified
  CSimpleServer *GetDataServer(void);

  // gets control server
  // @return : control server or NULL if not specified
  CSimpleServer *GetControlServer(void);

  // gets RTSP transport response header
  // @return : RTSP transport response header or NULL if not specified
  CRtspTransportResponseHeader *GetTransportResponseHeader(void);

  // gets last receiver report time
  // @return : last receiver report time
  DWORD GetLastReceiverReportTime(void);

  // gets receiver report interval
  // @return : receiver report interval
  DWORD GetReceiverReportInterval(void);

  // gets track synchronization source identifier
  // SSRC for track is generated when created class
  // @return : synchronization source identifier
  unsigned int GetSynchronizationSourceIdentifier(void);

  // gets sender synchronization source identifier
  // SSRC for track is generated when created class
  // @return : sender synchronization source identifier
  unsigned int GetSenderSynchronizationSourceIdentifier(void);

  // gets RTSP track statistical information
  // @return : RTSP strack statistical information
  CRtspTrackStatistics *GetStatistics(void);

  // gets payload type from media description
  // @return : payload type
  CPayloadType *GetPayloadType(void);

  // gets received RTP packets for current track
  // @return : RTP packets for current track
  CRtpPacketCollection *GetRtpPackets(void);

  /* set methods */

  // sets server data port
  // @param serverDataPort : server data port to set
  void SetServerDataPort(unsigned int serverDataPort);

  // sets server control port
  // @param serverControlPort : server control port to set
  void SetServerControlPort(unsigned int serverControlPort);

  // sets client data port
  // @param clientDataPort : client data port to set
  void SetClientDataPort(unsigned int clientDataPort);

  // sets client control port
  // @param clientControlPort : client control port to set
  void SetClientControlPort(unsigned int clientControlPort);

  // sets track URL
  // @param trackUrl : track URL to set
  // @return : true if successful, false otherwise
  bool SetTrackUrl(const wchar_t *trackUrl);

  // sets data server to track
  // @param dataServer : data server to set
  // @return : true if successful, false otherwise
  void SetDataServer(CSimpleServer *dataServer);

  // sets control server to track
  // @param controlServer : control server to set
  // @return : true if successful, false otherwise
  void SetControlServer(CSimpleServer *controlServer);

  // sets RTSP transport response header
  // @param header : RTSP transport response header to set
  // @return : true if successful, false otherwise
  bool SetTransportResponseHeader(CRtspTransportResponseHeader *header);

  // sets last receiver report time
  // @param lastReceiverReportTime : last receiver report time to set
  void SetLastReceiverReportTime(DWORD lastReceiverReportTime);

  // sets receiver report interval
  // @param receiverReportInterval : receiver report interval to set
  void SetReceiverReportInterval(DWORD receiverReportInterval);

  // sets synchronization source identifier
  // @param synchronizationSourceIdentifier : synchronization source identifier to set
  void SetSynchronizationSourceIdentifier(unsigned int synchronizationSourceIdentifier);

  // sets sender synchronization source identifier
  // @param senderSynchronizationSourceIdentifier : sender synchronization source identifier to set
  void SetSenderSynchronizationSourceIdentifier(unsigned int senderSynchronizationSourceIdentifier);

  // sets end of stream flag
  // @param endOfStream : the end of stream flag to set
  void SetEndOfStream(bool endOfStream);

  /* other methods */

  // tests if specified port is server data port
  // @param port : the port to test
  // @return : true if tested port is server data port, false otherwise
  bool IsServerDataPort(unsigned int port);

  // tests if specified port is client data port
  // @param port : the port to test
  // @return : true if tested port is client data port, false otherwise
  bool IsClientDataPort(unsigned int port);

  // tests if specified port is server control port
  // @param port : the port to test
  // @return : true if tested port is server control port, false otherwise
  bool IsServerControlPort(unsigned int port);

  // tests if specified port is client control port
  // @param port : the port to test
  // @return : true if tested port is client control port, false otherwise
  bool IsClientControlPort(unsigned int port);

  // tests if sender synchronization source identifier is set or not
  // @return : true if SSRC is set, false otherwise
  bool IsSetSenderSynchronizationSourceIdentifier(void);

  // deeply clones current instance
  // curl handle is not cloned
  // @result : deep clone of current instance or NULL if error
  CRtspTrack *Clone(void);

  // tests if end of stream is set
  // @return : true if end of stream is set, false otherwise
  bool IsEndOfStream(void);

  // tests if specific combination of flags is set
  // @return : true if specific combination of flags is set, false otherwise
  bool IsFlags(unsigned int flags);

protected:
  // holds flags
  unsigned int flags;

  // holds remote server data and control ports
  unsigned int serverDataPort;
  unsigned int serverControlPort;

  // holds our data and server ports
  unsigned int clientDataPort;
  unsigned int clientControlPort;

  // holds track URL
  wchar_t *trackUrl;

  // holds data and control server (if necessary)
  CSimpleServer *dataServer;
  CSimpleServer *controlServer;

  // holds RTSP transport response header (mostly for interleaved channels)
  CRtspTransportResponseHeader *transportResponseHeader;

  // holds last receiver report time
  DWORD lastReceiverReportTime;
  // holds receiver report interval
  DWORD receiverReportInterval;

  // holds track SSRC
  unsigned int synchronizationSourceIdentifier;
  // holds sender SSRC
  unsigned int senderSynchronizationSourceIdentifier;

  CRtspTrackStatistics *statistics;

  // holds payload type from SDP media description
  CPayloadType *payloadType;

  // holds collection of received and unprocessed RTP packets
  CRtpPacketCollection *rtpPackets;
};

#endif