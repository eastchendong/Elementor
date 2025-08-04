using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScriptBoy.ProceduralBook
{
    [AddComponentMenu(" Script Boy/Procedural Book/Wiro Book Binding")]
    public sealed class WiroBookBinding : BookBinding
    {
        [SerializeField] WireSetup m_WireSetup;

        internal override BookBound CreateBound(Book book, Transform root, RendererFactory rendererFactory, MeshFactory meshFactory)
        {
            return new WiroBookBound(m_WireSetup, book, root, rendererFactory, meshFactory);
        }

        class WiroBookBound : BookBound
        {
            Renderer m_WireRenderer;

            const float kBindingAngle = 45f + 22.5f;
            float m_BindingRadius;
            float m_StackHeight;
            float m_PaperZAngle;

            float m_WireLoopRadius;
            float m_WireThickness;
            float m_WireMargin;
            float m_WireTwinLoopSpace;
            float m_WireGap;
            int m_WireTwinLoopCount;

            internal override bool useSharedMeshDataForLowpoly => true;
            internal override Renderer binderRenderer => m_WireRenderer;

            public WiroBookBound(WireSetup wireSetup, Book book, Transform root, RendererFactory rendererFactory, MeshFactory meshFactory) : base(book, root)
            {
                if (book.totalThickness > book.minPaperWidth * 4)
                {
                    throw new BookHeightException();
                }

                var coverSetup = m_Book.coverPaperSetup;
                var paperSetup = m_Book.pagePaperSetup;

                Material wireMaterial = BookResources.FixNullMetalMaterial(wireSetup.material);
                m_WireRenderer = rendererFactory.Get("Wire");
                m_WireRenderer.castShadows = m_Book.castShadows;
                m_WireRenderer.SetMaterials(wireMaterial);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor(MaterialUtility.GetMainColorID(wireMaterial), wireSetup.color);
                m_WireRenderer.SetPropertyBlock(block, 0);
                m_WireRenderer.mesh = CreateWireMesh(wireSetup, meshFactory);
                m_WireRenderer.transform.localPosition = new Vector3(0, 0, m_WireMargin + paperSetup.margin + coverSetup.margin);

                m_Root.localPosition = new Vector3(0, m_WireLoopRadius + m_WireThickness / 2, m_Root.localPosition.z);

                float minTurningRadius = coverSetup.thickness * 2;
                minTurningRadius = Mathf.Max(minTurningRadius, m_WireLoopRadius);


                foreach (var paper in m_Book.papers)
                {
                    paper.SetMinTurningRadius(minTurningRadius);
                    paper.UpdateTurningRadius();
                    ResetPaperPosition(paper);
                }
            }

            Mesh CreateWireMesh(WireSetup wireSetup, MeshFactory meshFactory)
            {
                m_StackHeight = m_Book.totalThickness;
                m_WireThickness = wireSetup.thickness;
                m_BindingRadius = Mathf.Max(0, (m_StackHeight / 2) / Mathf.Sin((kBindingAngle) * Mathf.Deg2Rad));
                m_BindingRadius += m_WireThickness;

                float h = m_StackHeight / 2;
                float h1 = h;
                float r1 = m_BindingRadius - m_WireThickness / 2;
                float vx1 = Mathf.Sqrt(r1 * r1 - h1 * h1);
                vx1 -= Mathf.Max(m_Book.maxPaperThickness, m_WireThickness);
                m_BindingRadius += Mathf.Max(m_Book.minPaperThickness * 0.75f - vx1, 0);

                // r = h / s
                //h = r * s

                float h2 = m_BindingRadius - m_StackHeight / 2 + m_WireThickness / 2;
                var papers = m_Book.papers;
                float w2 = papers[0].size.x;
                float b2 = Mathf.Sqrt(w2 * w2 - h2 * h2);
                m_PaperZAngle = Mathf.Asin(b2 / w2) * Mathf.Rad2Deg - 90;



                m_WireLoopRadius = m_BindingRadius;


                m_WireTwinLoopSpace = Mathf.Lerp(m_WireThickness * 0.5f, m_WireThickness * 1.5f, wireSetup.twinLoopSpace);

                float cornerRadius0 = m_WireThickness * 2;
                float cornerRadius1 = m_WireTwinLoopSpace;

                int twinLoopCount = wireSetup.twinLoopCount;

                float length = m_Book.minPaperHeight;

                m_WireMargin = Mathf.Lerp(0, (length - (cornerRadius0 + cornerRadius1) * 4) / 2, wireSetup.margin);
                m_WireMargin = Mathf.Max(m_WireMargin, 0);
                length -= m_WireMargin * 2;

                float gap = (length - ((cornerRadius0 + cornerRadius1) * 2 * twinLoopCount)) / (twinLoopCount - 1);
                while (gap < 0 && twinLoopCount > 2)
                {
                    twinLoopCount--;
                    gap = (length - ((cornerRadius0 + cornerRadius1) * 2 * twinLoopCount)) / (twinLoopCount - 1);
                }

                while (gap < 0 && cornerRadius1 > 0)
                {
                    cornerRadius1 *= 0.99f;
                    gap = (length - ((cornerRadius0 + cornerRadius1) * 2 * twinLoopCount)) / (twinLoopCount - 1);
                }
                gap = Mathf.Max(gap, 0);

                m_WireTwinLoopSpace = cornerRadius1;
                m_WireGap = gap;
                m_WireTwinLoopCount = twinLoopCount;

                float qualityTime = wireSetup.quality / 5f;
                int basePointCount = (int)Mathf.Lerp(4, 20, qualityTime);
                int cornerPointCount0 = (int)Mathf.Lerp(4, 10, qualityTime);
                int cornerPointCount1 = (int)Mathf.Lerp(3, 10, qualityTime);
                int circlePointCount = (int)Mathf.Lerp(10, 50, qualityTime);


                int ringCount = twinLoopCount;
                float ringSpace = gap;
                float radius = m_WireLoopRadius;
                float baseRadius = m_WireThickness / 2;


                int verticesCapacity = basePointCount * 2 + ringCount * ((circlePointCount + cornerPointCount0 + cornerPointCount1) * 2 - 1) * basePointCount;
                int trianglesCapacity = (basePointCount - 2) * 3 * 2 + (ringCount * ((circlePointCount + cornerPointCount0 + cornerPointCount1) * 2 - 1) - 1) * basePointCount * 3 * 2;

                List<Vector3> vertices = new List<Vector3>(verticesCapacity);
                List<Vector3> normals = new List<Vector3>(verticesCapacity);
                List<int> triangles = new List<int>(trianglesCapacity);


                float width = cornerRadius0 + cornerRadius1;

                Vector3[] baseCircle = new Vector3[basePointCount];
                for (int i = 0; i < basePointCount; i++)
                {
                    float z = 90 - i * (360f / basePointCount);
                    Quaternion q = Quaternion.Euler(0, 0, z);
                    baseCircle[i] = q * Vector3.right;
                }

                Vector3 center = new Vector3(0, radius);
                for (int i = 0; i < basePointCount; i++)
                {
                    vertices.Add(baseCircle[i] * baseRadius - center);
                    normals.Add(Vector3.back);
                }

                for (int i = 2; i < basePointCount; i++)
                {
                    triangles.Add(0);
                    triangles.Add(i - 1);
                    triangles.Add(i);
                }


                int basePointsCapacity = (circlePointCount + cornerPointCount0 + cornerPointCount1) * 2 - 1;
                List<Vector3> basePoints = new List<Vector3>(basePointsCapacity);

                for (int i = 0; i < cornerPointCount0; i++)
                {
                    float t = i / (cornerPointCount0 - 1f);
                    float z = Mathf.Lerp(-90, 0, t);
                    Quaternion q = Quaternion.Euler(0, z, 0);
                    Vector3 p = q * new Vector3(0, 0, cornerRadius0);
                    p.x += cornerRadius0;
                    basePoints.Add(p);
                }

                float height = 2 * radius * Mathf.PI;

                for (int i = 0; i < circlePointCount; i++)
                {
                    float t = (i + 1f) / (circlePointCount + 1f);
                    float x = Mathf.Lerp(cornerRadius0, height - cornerRadius1, t);
                    Vector3 p = new Vector3(x, 0, cornerRadius0);
                    basePoints.Add(p);
                }

                for (int i = 0; i < cornerPointCount1; i++)
                {
                    float t = i / (cornerPointCount1 - 1f);
                    float z = Mathf.Lerp(180, 90, t);
                    Quaternion q = Quaternion.Euler(0, z, 0);
                    Vector3 p = q * new Vector3(0, 0, cornerRadius1);
                    p.z += width;
                    p.x += height - cornerRadius1;
                    basePoints.Add(p);
                }

                for (int i = 0; i < basePoints.Count; i++)
                {
                    Vector3 p = basePoints[i];
                    float rad = p.x;
                    float z = Mathf.Rad2Deg * (rad / radius) - 90;
                    Quaternion q = Quaternion.Euler(0, 0, z);
                    Vector3 v = q * new Vector3(radius, 0);
                    v.y += radius;
                    v.z = p.z;
                    basePoints[i] = v;
                }

                int l = basePoints.Count;
                for (int i = 1; i < l; i++)
                {
                    Vector3 v = basePoints[l - i - 1];
                    v.z = width * 2 - v.z;
                    basePoints.Add(v);
                }


                int n = basePoints.Count;
                int n3 = n * basePointCount;
                Vector3[] baseVertices = new Vector3[n3];
                Vector3[] baseNormals = new Vector3[n3];
                int w = 0;

                for (int i = 0; i < n; i++)
                {
                    Vector3 prev = basePoints[LoopUtility.PrevIndex(i, n)];
                    Vector3 current = basePoints[i];
                    Vector3 next = basePoints[LoopUtility.NextIndex(i, n)];

                    Vector3 forward;

                    if (i == 0)
                    {
                        forward = Vector3.forward;
                    }
                    else if (i == n - 1)
                    {
                        forward = Vector3.forward;
                    }
                    else
                    {
                        forward = ((current - prev).normalized + (next - current).normalized) / 2;
                    }

                    Vector3 upward = -(new Vector3(current.x, current.y) - new Vector3(center.x, center.y));
                    Quaternion q = forward == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(forward, upward);


                    float t = i / (n - 1f);

                    for (int j = 0; j < basePointCount; j++)
                    {
                        Vector3 normal = q * (baseCircle[j]);
                        baseVertices[w] = current + normal * baseRadius - center;
                        baseNormals[w] = normal;
                        w++;
                    }
                }

                vertices.AddRange(baseVertices);
                normals.AddRange(baseNormals);

                for (int j = 0; j < ringCount - 1; j++)
                {
                    for (int i = 0; i < baseVertices.Length; i++)
                    {
                        Vector3 v = baseVertices[i];
                        v.z += width * 2 + ringSpace;
                        baseVertices[i] = v;
                    }
                    vertices.AddRange(baseVertices);
                    normals.AddRange(baseNormals);
                }

                int n2 = basePoints.Count * ringCount;
                for (int i = 0; i < n2 - 1; i++)
                {
                    int iCurrent = i * basePointCount;
                    int iNext = (i + 1) * basePointCount;
                    for (int j = 0; j < basePointCount; j++)
                    {
                        int jNext = LoopUtility.NextIndex(j, basePointCount);

                        int a = basePointCount + iCurrent + j;
                        int b = basePointCount + iCurrent + jNext;
                        int c = basePointCount + iNext + jNext;
                        int d = basePointCount + iNext + j;

                        triangles.Add(a);
                        triangles.Add(d);
                        triangles.Add(b);

                        triangles.Add(b);
                        triangles.Add(d);
                        triangles.Add(c);
                    }
                }

                int offset = vertices.Count;
                for (int i = 0; i < basePointCount; i++)
                {
                    Vector3 p = baseCircle[i] * baseRadius;
                    p.z = (ringCount - 1) * (width * 2 + ringSpace) + width * 2;
                    vertices.Add(p - center);
                    normals.Add(Vector3.forward);
                }

                for (int i = 2; i < basePointCount; i++)
                {
                    triangles.Add(offset + 0);
                    triangles.Add(offset + i);
                    triangles.Add(offset + i - 1);
                }

                Mesh mesh = meshFactory.Get();
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
                return mesh;
            }

            internal override PaperPattern CreatePaperPattern(int quality, Vector2 size, float thickness, PaperUVMargin uvMargin, bool reduceOverdraw, bool reduceSubMeshes)
            {
                PaperPattern pattern = new PaperPattern();
                pattern.size = size;
                pattern.thickness = thickness;

                int holeCount = m_WireTwinLoopCount;
                float ringSpace = m_WireGap;
                float cornerR0 = m_WireThickness * 2;
                float cornerR1 = m_WireTwinLoopSpace;
                float baseR = m_WireThickness / 2;
                float wireMargin = (size.y - (holeCount * (cornerR0 + cornerR1) * 2 + ringSpace * (holeCount - 1))) / 2;
                int zSpaceCount = holeCount - 1;
                cornerR0 -= baseR * 2;
                cornerR1 += baseR * 2;

                baseR = Mathf.Max(baseR, thickness / 2);
                float x1 = baseR * 2;

                float h = m_StackHeight / 2;
                float h0 = h - thickness;
                float h1 = h;
                float r0 = m_BindingRadius + m_WireThickness / 2;
                float r1 = m_BindingRadius - m_WireThickness / 2;
                float vx0 = Mathf.Sqrt(r0 * r0 - h0 * h0);
                float vx1 = Mathf.Sqrt(r1 * r1 - h1 * h1);
                float x2 = x1 + (vx0 - vx1);

                float qualityTime = quality / 5f;
                float s = Mathf.Min(size.x, size.y) / 60;
                int subdivideXSpace = (int)Mathf.Lerp(0, (size.x - x2) / s, qualityTime);
                int subdivideZSpace = (int)Mathf.Lerp(0, (ringSpace + cornerR0 * 2) / s, qualityTime);
                int subdivideZHole = (int)Mathf.Lerp(0, (cornerR1 * 2) / s, qualityTime);
                int subdivideZSpace0 = (int)Mathf.Lerp(0, (cornerR0 + wireMargin) / s, qualityTime);

                int nX = 3 + subdivideXSpace + 1;
                int nZ = 2 + holeCount * 2 + holeCount * subdivideZHole + zSpaceCount * subdivideZSpace + subdivideZSpace0 * 2;


                PaperNode xRootNode = new PaperNode(0);
                PaperNode xCurrentNode = xRootNode;

                xCurrentNode = xCurrentNode.CreateNext(x1, true);
                PaperNode xHoleNode = xCurrentNode;

                xCurrentNode = xCurrentNode.CreateNext(x2, false);

                double xStep = (size.x - x2) / (subdivideXSpace + 1);
                double xValue = x2 + xStep;
                for (int i = 3; i < nX - 1; i++)
                {
                    xCurrentNode = xCurrentNode.CreateNext(xValue);
                    xValue += xStep;
                }
                xCurrentNode = xCurrentNode.CreateNext(size.x);


                PaperNode zRootNode = new PaperNode(0);
                PaperNode zCurrentNode = zRootNode;

                List<PaperNode> zHoleNodes = new List<PaperNode>();

                double zSpace0Step = (cornerR0 + wireMargin) / (subdivideZSpace0 + 1);
                double zSpaceStep = (cornerR0 * 2 + ringSpace) / (subdivideZSpace + 1);
                double zHoleStep = (cornerR1 * 2) / (subdivideZHole + 1);
                double zValue = 0;
                for (int j = 0; j < subdivideZSpace0 + 1; j++)
                {
                    zValue += zSpace0Step;
                    zCurrentNode = zCurrentNode.CreateNext(zValue);
                }

                zCurrentNode.hole = true;
                zHoleNodes.Add(zCurrentNode);
                for (int j = 0; j < subdivideZHole + 1; j++)
                {
                    zValue += zHoleStep;
                    zCurrentNode = zCurrentNode.CreateNext(zValue, true);
                }
                zCurrentNode.hole = false;

                for (int j = 0; j < zSpaceCount; j++)
                {
                    for (int w = 0; w < subdivideZSpace + 1; w++)
                    {
                        zValue += zSpaceStep;
                        zCurrentNode = zCurrentNode.CreateNext(zValue);
                    }

                    zCurrentNode.hole = true;
                    zHoleNodes.Add(zCurrentNode);
                    for (int w = 0; w < subdivideZHole + 1; w++)
                    {
                        zValue += zHoleStep;
                        zCurrentNode = zCurrentNode.CreateNext(zValue, true);
                    }
                    zCurrentNode.hole = false;
                }
                for (int j = 0; j < subdivideZSpace0 + 1; j++)
                {
                    zValue += zSpace0Step;
                    zCurrentNode = zCurrentNode.CreateNext(zValue);
                }


                List<PaperNode> xSeemNodes = new List<PaperNode>();
                List<PaperNode> zSeemNodes = new List<PaperNode>();

                PaperUVMargin margin = uvMargin;

                if (reduceOverdraw)
                {
                    int i = 0;
                    if (m_Book.hasCover)
                    {
                        i = m_Book.coverPaperCount / 2;
                    }

                    float w = Mathf.Max(0.01f, thickness);
                    float o = GetPX(i + 1, thickness) - GetPX(i, thickness);

                    margin.left = (x2 + 0.01f + o) / size.x;
                    margin.right = (GetPX(i + 1, thickness) - GetPX(i, thickness) + w) / size.x;


                    margin.down = w / size.y;
                    margin.up = w / size.y;
                }

                PaperNodeMargin uvNodeMargin = new PaperNodeMargin(pattern, margin, false);



  
                uvNodeMargin.Insert(xRootNode, zRootNode, xSeemNodes, zSeemNodes);

       
                xRootNode.UpdateIndex(0);
                zRootNode.UpdateIndex(0);

                PaperMeshUtility.SeamNodesToSeams(xSeemNodes, pattern.xSeams);
                PaperMeshUtility.SeamNodesToSeams(zSeemNodes, pattern.zSeams);


                List<float> xList = xRootNode.GetValues();
                List<float> zList = zRootNode.GetValues();

                List<bool> xHoles = xRootNode.GetHoles();
                List<bool> zHoles = zRootNode.GetHoles();

                nX = xList.Count;
                nZ = zList.Count;
                int baseVertexCount = nX * nZ;

                List<Vector2> texcoords = new List<Vector2>();
                int[] weights = new int[baseVertexCount];
                List<int> frontTriangles = new List<int>();
                List<int> backTriangles = new List<int>();
                List<int> borderTriangles = new List<int>();
                List<PaperBorder> borders = new List<PaperBorder>();

   
                PaperMeshUtility.AddFrontAndBackTexcoords(texcoords, xList, zList, size, uvMargin, m_Book.direction);

                borders.Add(new PaperBorder(0, 0, nX - 1, nZ - 1, false));


                int xHoleStart = uvNodeMargin.leftNode.index;
                int zHoleStart = uvNodeMargin.downNode.index;

                int xHoleEnd = uvNodeMargin.rightNode.index;
                int zHoleEnd = uvNodeMargin.upNode.index;

                if (xHoleEnd == 0) xHoleEnd = nX - 1;
                if (zHoleEnd == 0) zHoleEnd = nZ - 1;


                for (int z = 0; z < nZ - 1; z++)
                {
                    for (int x = 0; x < nX - 1; x++)
                    {
                        if (reduceOverdraw)
                        {
                            if (z >= zHoleStart && z < zHoleEnd && x >= xHoleStart && x < xHoleEnd) continue;
                        }

                        int a = (z) * nX + (x);
                        int b = (z) * nX + (x + 1);
                        int c = (z + 1) * nX + (x);
                        int d = (z + 1) * nX + (x + 1);

                        weights[a] += 2;
                        weights[b] += 2;
                        weights[c] += 2;
                        weights[d] += 2;


                        if (xHoles[x] && zHoles[z]) continue;

                        PaperMeshUtility.AddFrontAndBackFaces(frontTriangles, backTriangles, a, b, c, d, baseVertexCount);
                    }
                }

                int holeStartX = xHoleNode.index;
                int holeEndX = xHoleNode.nextNoneHole.index;

                foreach (var zHoleNode in zHoleNodes)
                {
                    int holeStartZ = zHoleNode.index;
                    int holeEndZ = zHoleNode.nextNoneHole.index;

                    borders.Add(new PaperBorder(holeStartX, holeStartZ, holeEndX, holeEndZ, true));
                }

                PaperMeshUtility.AddBorders(borders, borderTriangles, texcoords, nX, nZ);


                pattern.baseXArray = xList.ToArray();
                pattern.baseZArray = zList.ToArray();
                pattern.baseVertexCount = baseVertexCount;
                pattern.vertexCount = texcoords.Count;
                pattern.texcoords = texcoords.ToArray();
                pattern.weights = weights;
                if (reduceSubMeshes)
                {
                    pattern.subMeshCount = 1;
                    List<int> triangles = new List<int>(frontTriangles.Count + backTriangles.Count + borderTriangles.Count);
                    triangles.AddRange(frontTriangles);
                    triangles.AddRange(borderTriangles);
                    triangles.AddRange(backTriangles);
                    pattern.triangles = triangles.ToArray();
                }
                else
                {
                    pattern.subMeshCount = 3;
                    pattern.frontTriangles = frontTriangles.ToArray();
                    pattern.backTriangles = backTriangles.ToArray();
                    pattern.borderTriangles = borderTriangles.ToArray();
                }
                pattern.borders = borders.ToArray();

                return pattern;
            }


            float GetPX(int index, float thickness)
            {

                float h3 = GetStackHeight(index) - thickness / 2;
                float rightStackZ = GetStackZ(h3);
                float leftStackZ = 180 + rightStackZ;
                bool isFlipped = false;
                float t = isFlipped ? 1 : 0;
                float z = Mathf.Lerp(rightStackZ, leftStackZ, t);
                Vector3 p = Quaternion.Euler(0, 0, z) * Vector3.right * m_BindingRadius;
                Vector3 direction = isFlipped ? Vector3.right : Vector3.left;
                p += direction * GetHoleOffset(index, thickness);

                return p.x;
            }

            internal override void ResetPaperPosition(Paper paper)
            {
                float h = GetStackHeight(paper.index) - paper.thickness / 2;
                float rightStackZ = GetStackZ(h);
                float leftStackZ = 180 + rightStackZ;

                bool isFlipped = paper.isFlipped;
                float t = isFlipped ? 1 : 0;
                float z = Mathf.Lerp(rightStackZ, leftStackZ, t);
                Vector3 p = Quaternion.Euler(0, 0, z) * Vector3.right * m_BindingRadius;
                Vector3 direction = isFlipped ? Vector3.right : Vector3.left;
                p += direction * GetHoleOffset(paper);
                p.z = paper.margin;
                paper.transform.localPosition = p;


                z = 0;
                if (m_Book.alignToGround)
                {
                    z = Mathf.Lerp(m_PaperZAngle, -m_PaperZAngle, t);
                }

                paper.transform.localEulerAngles = new Vector3(0, 0, z);

            }

            internal override void UpdatePaperPosition(Paper paper)
            {
                float h = GetStackHeight(paper.index) - paper.thickness / 2;
                float rightStackZ = GetStackZ(h);
                float leftStackZ = 180 + rightStackZ;


                paper.UpdateTime();
                float t = paper.zTime;
                var curve = new AnimationCurve(new Keyframe(0, rightStackZ), new Keyframe(0.5f, 90), new Keyframe(1, leftStackZ));
                float z = curve.Evaluate(t);

                Vector3 p = Quaternion.Euler(0, 0, z) * Vector3.right * m_BindingRadius;

                p += paper.direction * GetHoleOffset(paper);



                bool flip = paper.isFlipped;
                float minY = (Quaternion.Euler(0, 0, flip ? leftStackZ : rightStackZ) * Vector3.right * m_BindingRadius).y;
                if (flip && t > 0.5f || !flip && t < 0.5f)
                {
                    p.y = Mathf.Max(p.y, minY);
                }

                p.z = paper.margin;
                paper.transform.localPosition = p;

                z = 0;
                if (m_Book.alignToGround)
                {
                    z = Mathf.Lerp(m_PaperZAngle, -m_PaperZAngle, t);
                }

                paper.transform.localEulerAngles = new Vector3(0, 0, z);
            }

            internal override void OnLateUpdate()
            {
                foreach (var paper in m_Book.papers)
                {
                    if (paper.isTurning || paper.isFalling) paper.UpdateMesh();
                }
            }

            float GetHoleOffset(Paper paper)
            {
                float h = GetStackMidHeight(paper.index);
                float h0 = h - paper.thickness / 2;
                float h1 = h;
                float r0 = m_BindingRadius;
                float r1 = m_BindingRadius - m_WireThickness / 2;
                float x0 = Mathf.Sqrt(r0 * r0 - h0 * h0);
                float x1 = Mathf.Sqrt(r1 * r1 - h1 * h1);
                float baseR = Mathf.Max(paper.thickness, m_WireThickness);
                return baseR + x0 - x1;
            }

            float GetHoleOffset(int index, float thickness)
            {
                float h = GetStackMidHeight(index);
                float h0 = h - thickness / 2;
                float h1 = h;
                float r0 = m_BindingRadius;
                float r1 = m_BindingRadius - m_WireThickness / 2;
                float x0 = Mathf.Sqrt(r0 * r0 - h0 * h0);
                float x1 = Mathf.Sqrt(r1 * r1 - h1 * h1);
                float baseR = Mathf.Max(thickness, m_WireThickness);
                return baseR + x0 - x1;
            }

            float GetStackHeight(int startIndex)
            {
                var papers = m_Book.papers;
                float h = 0;
                int n = papers.Length;
                for (int i = startIndex; i < n; i++)
                {
                    h += papers[i].thickness;
                }
                return h;
            }

            float GetStackMidHeight(int index)
            {
                var papers = m_Book.papers;
                float h = 0;
                int n = papers.Length;
                int n2 = n / 2;
                if (index < n2)
                {
                    for (int i = index; i < n2; i++)
                    {
                        h += papers[i].thickness;
                    }
                }
                else
                {
                    for (int i = n2; i <= index; i++)
                    {
                        h += papers[i].thickness;
                    }
                }
                return h;
            }

            float GetStackZ(float stackHeight)
            {
                float h = stackHeight - m_StackHeight * 0.5f;
                return Mathf.Asin(h / m_BindingRadius) * Mathf.Rad2Deg;
            }
        }
    }

    [System.Serializable]
    class WireSetup
    {
        const float kMinThickness = 0.02f;
        const float kMaxThickness = 0.1f;
        const int kMinCount = 2;
        const int kMaxCount = 10;
        const int kMinQuality = 0;
        const int kMaxQuality = 5;

        [SerializeField]
        Material m_Material;

        [SerializeField]
        Color m_Color;

        [SerializeField, Range(kMinThickness, kMaxThickness)]
        float m_Thickness;

        [Tooltip("The blank space between the wire and the book's horizontal edges.")]
        [SerializeField, Range(0, 1)]
        float m_Margin;

        [Tooltip("The space between two connected wire loops that form a single continuous loop.")]
        [SerializeField, Range(0, 1)]
        float m_TwinLoopSpace;

        [Tooltip("The number of the twin loops.")]
        [SerializeField, Range(kMinCount, kMaxCount)]
        int m_TwinLoopCount;

        [Tooltip("The quality level of the wire mesh.")]
        [SerializeField, Range(kMinQuality, kMaxQuality)]
        int m_Quality;


        public WireSetup()
        {
            color = Color.white;
            thickness = 0.05f;
            margin = 0.1f;
            twinLoopSpace = 0.25f;
            twinLoopCount = 4;
            quality = 3;
        }

        public Material material
        {
            get => m_Material;
            set => m_Material = value;
        }

        public Color color
        {
            get => m_Color;
            set => m_Color = value;
        }

        public float thickness
        {
            get => m_Thickness;
            set => m_Thickness = Mathf.Clamp(m_Thickness, kMinThickness, kMaxThickness);
        }

        public float margin
        {
            get => m_Margin;
            set => m_Margin = Mathf.Clamp01(value);
        }

        public float twinLoopSpace
        {
            get => m_TwinLoopSpace;
            set => m_TwinLoopSpace = Mathf.Clamp01(value);
        }

        public int twinLoopCount
        {
            get => m_TwinLoopCount;
            set => m_TwinLoopCount = Mathf.Clamp(value, kMinCount, kMaxCount);
        }

        public int quality
        {
            get => m_Quality;
            set => m_Quality = Mathf.Clamp(value, kMinQuality, kMaxQuality);
        }
    }
}