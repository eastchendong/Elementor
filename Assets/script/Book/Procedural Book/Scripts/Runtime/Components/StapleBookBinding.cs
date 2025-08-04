using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    [AddComponentMenu(" Script Boy/Procedural Book/Staple Book Binding")]
    public sealed class StapleBookBinding : BookBinding
    {
        [Tooltip("The quality level of the paper mesh on the binding side.")]
        [Range(1, 5)]
        [SerializeField]
        int m_Quality = 3;

        [Space]
        [SerializeField]
        StapleSetup m_StapleSetup;

        internal override BookBound CreateBound(Book book, Transform root, RendererFactory rendererFactory, MeshFactory meshFactory)
        {
            return new StapleBookBound(m_Quality, m_StapleSetup, book, root, rendererFactory, meshFactory);
        }

        class StapleBookBound : BookBound
        {
            Renderer m_StapleRenderer;

            float m_StapleMargin;
            float m_StapleThickness;

            float m_BindingRadius;
            float m_BindingMidSpace;
            float m_StackHeight;
            int m_BindingVertexCount;

            int m_Quality;


            internal override bool useSharedMeshDataForLowpoly => false;

            internal override Renderer binderRenderer => m_StapleRenderer;

            public StapleBookBound(int quality, StapleSetup stapleSetup, Book book, Transform root, RendererFactory rendererFactory, MeshFactory meshFactory) : base(book, root)
            {
                if (book.totalThickness * 1.25f > book.minPaperWidth)
                {
                    throw new BookHeightException();
                }

                m_Quality = quality;
                PaperSetup coverSetup = m_Book.coverPaperSetup;
                PaperSetup paperSetup = m_Book.pagePaperSetup;

                m_StapleRenderer = rendererFactory.Get("Staple");
                m_StapleRenderer.castShadows = m_Book.castShadows;
                Material stapleMaterial = BookResources.FixNullMetalMaterial(stapleSetup.material);
                m_StapleRenderer.SetMaterials(stapleMaterial);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor(MaterialUtility.GetMainColorID(stapleMaterial), stapleSetup.color);
                m_StapleRenderer.SetPropertyBlock(block, 0);
                Mesh stapleMesh = meshFactory.Get();
                UpdateStapleMesh(stapleSetup, stapleMesh);
                m_StapleRenderer.mesh = stapleMesh;
                m_StapleRenderer.transform.localPosition = new Vector3(0, 0, m_StapleMargin + paperSetup.margin + coverSetup.margin);

                float minTurningRadius = coverSetup.thickness;
                minTurningRadius = Mathf.Max(m_BindingRadius, minTurningRadius);

                foreach (var paper in m_Book.papers)
                {
                    Vector2 size = paper.size;
                    size.x -= m_BindingRadius;
                    paper.sizeXOffset = m_BindingRadius;
                    paper.size = size;
                    paper.SetMinTurningRadius(minTurningRadius);
                    paper.UpdateTurningRadius();
                    UpdatePaperPosition(paper);
                }

                UpdateRootPosition();
            }

            void UpdateStapleMesh(StapleSetup stapleSetup, Mesh mesh)
            {
                m_StapleThickness = stapleSetup.thickness;

                m_StackHeight = m_Book.totalThickness;

                m_BindingMidSpace = m_StapleThickness * 1.75f;

                m_BindingRadius = ((m_StackHeight + m_BindingMidSpace) / 2) / Mathf.Sin(45 * Mathf.Deg2Rad);
                m_StackHeight += m_BindingMidSpace;
                float crown = stapleSetup.crown;
                crown = Mathf.Max(crown, m_StapleThickness * 4);

                float minMargin = m_StapleThickness * 0.5f;
                float maxMargin = Mathf.Max(m_Book.minPaperHeight / 2 - crown - minMargin, minMargin);

                m_StapleMargin = Mathf.Lerp(minMargin, maxMargin, stapleSetup.margin);

                float length = m_Book.minPaperHeight - m_StapleMargin * 2;
                int stapleCount = stapleSetup.count;
                float space = (length - crown * stapleCount) / (stapleCount - 1);
                space = Mathf.Max(space, 0);
                while (space < m_StapleThickness * 2 && stapleCount > 2)
                {
                    stapleCount--;
                    space = (length - crown * stapleCount) / (stapleCount - 1);
                    space = Mathf.Max(space, 0);
                }

                float qualityTime = stapleSetup.quality / 5f;
                int basePointCount = (int)Mathf.Lerp(4, 20, qualityTime);
                int cornerPointCount0 = (int)Mathf.Lerp(4, 10, qualityTime);
                int cornerPointCount1 = (int)Mathf.Lerp(3, 10, qualityTime);
                float baseRadius = m_StapleThickness / 2f;
                float teethH = baseRadius * 2.5f;
                float teethT = 0.9f;
                float cornerRadius0 = baseRadius * 1.0f;
                float cornerRadius1 = (crown / 2) * teethT;
                cornerRadius1 = Mathf.Max(cornerRadius1, cornerRadius0 * 2);
                float leg = 0;
                leg += m_Book.totalThickness / 2;
                leg += baseRadius;
                float xOffset = -(m_Book.papers[0].thickness / 2 + baseRadius);

                List<Vector3> baseTubePoints = new List<Vector3>();
                {
                    for (int i = 0; i < cornerPointCount0; i++)
                    {
                        float t = i / (cornerPointCount0 - 1f);
                        float z = Mathf.Lerp(-90, -180, t);
                        Quaternion q = Quaternion.Euler(0, z, 0);
                        Vector3 p = q * new Vector3(0, 0, cornerRadius0);
                        p.x += cornerRadius0;
                        p.z += cornerRadius0;
                        p.x += xOffset;
                        baseTubePoints.Add(p);
                    }

                    Vector3 a = new Vector3(leg, 0, 0);
                    Vector3 b = new Vector3(leg + teethH * 0.75f, 0, 0);
                    Vector3 c = new Vector3(leg - baseRadius * 0.5f, 0, cornerRadius1);

                    for (int i = 0; i < cornerPointCount1; i++)
                    {
                        float t = i / (cornerPointCount1 - 1f);
                        Vector3 p = BezierUtility.Evaluate(a, b, c, t);
                        p.x += xOffset;
                        baseTubePoints.Add(p);
                    }

                    baseTubePoints.Reverse();
                }

                Vector3[] baseCircle = new Vector3[basePointCount];
                for (int i = 0; i < basePointCount; i++)
                {
                    float z = 90 - i * (360f / basePointCount);
                    Quaternion q = Quaternion.Euler(0, 0, z);

                    baseCircle[i] = q * Vector3.right;
                    baseCircle[i].x *= 0.75f;
                }

                int n = baseTubePoints.Count;
                int n3 = n * basePointCount;
                Vector3[] baseVertices = new Vector3[n3 * 2];
                Vector3[] baseNormals = new Vector3[n3 * 2];
                int[] baseTriangles = new int[(n3 * 2 - 1) * basePointCount * 2 * 3];
                int w = 0;

                for (int i = 0; i < n; i++)
                {
                    Vector3 prev = baseTubePoints[LoopUtility.PrevIndex(i, n)];
                    Vector3 current = baseTubePoints[i];
                    Vector3 next = baseTubePoints[LoopUtility.NextIndex(i, n)];

                    Vector3 forward;

                    if (i == 0)
                    {
                        //forward = Vector3.right;
                        // forward = Vector3.back;
                        forward = new Vector3(1, 0, -2.0f).normalized;
                        //forward = -Vector3.left;
                        //  upward = Vector3.forward;
                    }
                    else if (i == n - 1)
                    {
                        forward = Vector3.forward;
                    }
                    else
                    {
                        forward = ((current - prev).normalized + (next - current).normalized) / 2;
                    }

                    Vector3 upward = Vector3.up;
                    Quaternion q = forward == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(forward, upward);

                    float t = i / (n - 1f);

                    int w2 = w;

                    float t2 = Mathf.InverseLerp(0, cornerPointCount1 / 4f, i);
                    t2 = Mathf.Lerp(0.1f, 1, t2);
                    t2 = Mathf.Sqrt(t2);
                    // t2 = t2 * t2;

                    for (int j = 0; j < basePointCount; j++)
                    {
                        Vector3 normal = q * (baseCircle[j]) * t2;
                        Vector3 vertex = current + normal * baseRadius;

                        baseVertices[w] = vertex;
                        baseNormals[w] = normal;

                        vertex = current + normal * baseRadius;
                        vertex.z = (crown) - vertex.z;

                        int jj = w2 + basePointCount - j - 1;
                        int ii = n3 * 2 - jj - 1;

                        baseVertices[ii] = vertex;
                        baseNormals[ii] = normal;

                        w++;
                    }
                }


                int jjj = 0;
                int n2 = baseTubePoints.Count * 2;
                for (int i = 0; i < n2 - 1; i++)
                {
                    int iCurrent = i * basePointCount;
                    int iNext = (i + 1) * basePointCount;
                    for (int j = 0; j < basePointCount; j++)
                    {
                        int jNext = LoopUtility.NextIndex(j, basePointCount);

                        int a = iCurrent + j;
                        int b = iCurrent + jNext;
                        int c = iNext + jNext;
                        int d = iNext + j;

                        baseTriangles[jjj++] = a;
                        baseTriangles[jjj++] = d;
                        baseTriangles[jjj++] = b;
                        baseTriangles[jjj++] = b;
                        baseTriangles[jjj++] = d;
                        baseTriangles[jjj++] = c;
                    }
                }


                List<Vector3> vertices = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                List<int> triangles = new List<int>();

                vertices.AddRange(baseVertices);
                normals.AddRange(baseNormals);
                triangles.AddRange(baseTriangles);

                for (int i = 0; i < stapleCount - 1; i++)
                {
                    int vc = baseVertices.Length;
                    for (int j = 0; j < baseTriangles.Length; j++)
                    {
                        baseTriangles[j] += vc;
                    }

                    for (int j = 0; j < baseVertices.Length; j++)
                    {
                        baseVertices[j].z += space + crown;
                    }

                    vertices.AddRange(baseVertices);
                    normals.AddRange(baseNormals);
                    triangles.AddRange(baseTriangles);
                }

                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
            }

            internal override PaperPattern CreatePaperPattern(int quality, Vector2 size, float thickness, PaperUVMargin uvMargin, bool reduceOverdraw, bool reduceSubMeshes)
            {
                PaperPattern pattern = new PaperPattern();
                pattern.size = size;
                pattern.thickness = thickness;

                float bindingRadius = m_BindingRadius;
                pattern.baseXOffset = -bindingRadius;

                int subdivideBindingAreaX = m_Quality + 1;
                m_BindingVertexCount = subdivideBindingAreaX;

                float qualityTime = quality / 5f;
                float s = Mathf.Min(size.x, size.y) / 60;
                int subdivideMainAreaX = (int)Mathf.Lerp(0, (size.x - bindingRadius) / s, qualityTime);
                int subdivideZ = (int)Mathf.Lerp(0, (size.y) / s, qualityTime);

                int nX = 2 + subdivideMainAreaX + 1 + subdivideBindingAreaX;
                int nZ = 2 + subdivideZ;

                PaperNode xRootNode = new PaperNode(0);
                PaperNode xCurrentNode = xRootNode;
                double xStep = bindingRadius / (subdivideBindingAreaX);
                double xValue = 0;
                for (int i = 1; i < subdivideBindingAreaX + 1; i++)
                {
                    xCurrentNode = xCurrentNode.CreateNext(xValue);
                    xValue += xStep;
                }
                xStep = (size.x - bindingRadius) / (subdivideMainAreaX + 1);
                for (int i = subdivideBindingAreaX + 1; i < nX; i++)
                {
                    xCurrentNode = xCurrentNode.CreateNext(xValue);
                    xValue += xStep;
                }

                PaperNode zRootNode = new PaperNode(0);
                PaperNode zCurrentNode = zRootNode;
                double zStep = size.y / (nZ - 1);
                double zValue = 0;
                for (int i = 0; i < nZ - 1; i++)
                {
                    zValue += zStep;
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

                    margin.left = 0;
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



                int n = m_BindingVertexCount + 2;
                int[] xNoneSeamIndexes = pattern.xNoneSeamIndexes = new int[n];
                xCurrentNode = xRootNode;
                for (int i = 0; i < n; i++)
                {
                    xNoneSeamIndexes[i] = xCurrentNode.index;
                    do
                    {
                        xCurrentNode = xCurrentNode.next;
                    } while (xCurrentNode.seam);
                }

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

                        PaperMeshUtility.AddFrontAndBackFaces(frontTriangles, backTriangles, a, b, c, d, baseVertexCount);
                    }
                }

                borders.Add(new PaperBorder(0, 0, nX - 1, nZ - 1, false, false));

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

            void UpdateRootPosition()
            {
                var papers = m_Book.papers;
                float y0 = papers[0].transform.localPosition.y;
                float y1 = papers[papers.Length - 1].transform.localPosition.y;
                float h = -Mathf.Min(y0, y1);
                h += papers[0].thickness / 2;
                m_Root.localPosition = new Vector3(0, h, m_Root.localPosition.z);
            }

            internal override void ResetPaperPosition(Paper paper)
            {
                var papers = m_Book.papers;
                int paperCount = papers.Length;
                float th = 0;
                int midIndex0 = papers.Length / 2 - 1;
                int midIndex1 = papers.Length / 2;
                for (int j = 0; j < paperCount; j++)
                {
                    var paper2 = papers[j];
                    paper2.UpdateTime();
                    float zTime = paper2.zTime;
                    float thickness = paper2.thickness;
                    th += zTime * thickness;

                    if (j == midIndex0)
                    {
                        th += zTime * m_BindingMidSpace / 2;
                    }

                    if (j == midIndex1)
                    {
                        th += zTime * m_BindingMidSpace / 2;
                    }
                }

                float h = GetStackHeight(paper.index) - paper.thickness / 2;
                float rightStackZ = GetStackZ(h);
                float leftStackZ = 180 + rightStackZ;
                float t = paper.isFlipped ? 1 : 0;
                float z = Mathf.Lerp(rightStackZ, leftStackZ, t);
                Vector3 p = Quaternion.Euler(0, 0, z) * Vector3.right * m_BindingRadius;
                p.z = paper.margin;
                float w = papers[paperCount / 2].size.x;
                float h2 = m_BindingMidSpace;
                float b = Mathf.Sqrt(w * w - h2 * h2);
                float z2 = Mathf.Asin(b / w) * Mathf.Rad2Deg - 90;
                int i = paper.index;

                if (i < paperCount / 2)
                {
                    float z3 = 0;
                    if (m_Book.alignToGround)
                    {
                        w = papers[0].size.x;
                        float midH = m_StackHeight / 2;
                        h2 = Mathf.Clamp(th, 0, midH) - midH;
                        b = Mathf.Sqrt(w * w - h2 * h2);
                        z3 = (Mathf.Asin(b / w) * Mathf.Rad2Deg - 90) * 2;
                    }
                    z = Mathf.Lerp(z2, -z3, t);
                }
                else
                {
                    float z3 = 0;
                    if (m_Book.alignToGround)
                    {
                        w = papers[0].size.x;
                        float midH = m_StackHeight / 2;
                        h2 = midH - Mathf.Clamp(th, midH, midH * 2);
                        b = Mathf.Sqrt(w * w - h2 * h2);
                        z3 = (Mathf.Asin(b / w) * Mathf.Rad2Deg - 90) * 2;
                    }
                    z = Mathf.Lerp(z3, -z2, t);
                }

                paper.transform.localPosition = p;
                paper.transform.localRotation = Quaternion.Euler(0, 0, z);
            }


            float GetPX(int index, float thickness)
            {
                var papers = m_Book.papers;
                int paperCount = papers.Length;
                float th = 0;
                int midIndex0 = papers.Length / 2 - 1;
                int midIndex1 = papers.Length / 2;
                for (int j = 0; j < paperCount; j++)
                {
  
                    float zTime = 0;
                    th += zTime * thickness;
                    if (j == midIndex0)
                    {
                        th += zTime * m_BindingMidSpace / 2;
                    }

                    if (j == midIndex1)
                    {
                        th += zTime * m_BindingMidSpace / 2;
                    }
                }

                float h = GetStackHeight(index) - thickness / 2;
                float rightStackZ = GetStackZ(h);
                float leftStackZ = 180 + rightStackZ;
                float t = 0;
                float z = Mathf.Lerp(rightStackZ, leftStackZ, t);
                Vector3 p = Quaternion.Euler(0, 0, z) * Vector3.right * m_BindingRadius;
   
                return p.x;
            }


            internal override void UpdatePaperPosition(Paper paper)
            {
                var papers = m_Book.papers;
                int paperCount = papers.Length;
                float th = 0;
                int midIndex0 = papers.Length / 2 - 1;
                int midIndex1 = papers.Length / 2;
                for (int j = 0; j < paperCount; j++)
                {
                    var paper2 = papers[j];
                    paper2.UpdateTime();
                    float zTime = paper2.zTime;
                    float thickness = paper2.thickness;
                    th += zTime * thickness;

                    if (j == midIndex0)
                    {
                        th += zTime * m_BindingMidSpace / 2;
                    }

                    if (j == midIndex1)
                    {
                        th += zTime * m_BindingMidSpace / 2;
                    }
                }

                float h = GetStackHeight(paper.index) - paper.thickness / 2;
                float rightStackZ = GetStackZ(h + th);
                float leftStackZ = 180 + GetStackZ(h + th - m_StackHeight);
                //rightStackZ += 45 * (time) * 2;
                //leftStackZ -= 45 * (1 - time) * 2;

                float t = paper.zTime;
                float z = Mathf.Lerp(rightStackZ, leftStackZ, t);
                Vector3 p = Quaternion.Euler(0, 0, z) * Vector3.right * m_BindingRadius;
                p.z = paper.margin;


                float w = papers[paperCount / 2].size.x;
                float h2 = m_BindingMidSpace;
                float b = Mathf.Sqrt(w * w - h2 * h2);
                float z2 = Mathf.Asin(b / w) * Mathf.Rad2Deg - 90;
                int i = paper.index;

                if (i < paperCount / 2)
                {
                    float z3 = 0;
                    if (m_Book.alignToGround)
                    {
                        w = papers[0].size.x;
                        float midH = m_StackHeight / 2;
                        h2 = Mathf.Clamp(th, 0, midH) - midH;
                        b = Mathf.Sqrt(w * w - h2 * h2);
                        z3 = (Mathf.Asin(b / w) * Mathf.Rad2Deg - 90) * 2;
                    }
                    z = Mathf.Lerp(z2, -z3, t);
                }
                else
                {
                    float z3 = 0;
                    if (m_Book.alignToGround)
                    {
                        w = papers[0].size.x;
                        float midH = m_StackHeight / 2;
                        h2 = midH - Mathf.Clamp(th, midH, midH * 2);
                        b = Mathf.Sqrt(w * w - h2 * h2);
                        z3 = (Mathf.Asin(b / w) * Mathf.Rad2Deg - 90) * 2;
                    }
                    z = Mathf.Lerp(z3, -z2, t);
                }

                paper.transform.localPosition = p;
                paper.transform.localRotation = Quaternion.Euler(0, 0, z);
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
                if (startIndex < n / 2) h += m_BindingMidSpace;
                return h;
            }

            float GetStackZ(float stackHeight)
            {
                stackHeight = Mathf.Clamp(stackHeight, 0, m_StackHeight);
                float h = stackHeight - m_StackHeight * 0.5f;
                return Mathf.Asin(h / m_BindingRadius) * Mathf.Rad2Deg;
            }

            internal override void OnLateUpdate()
            {
                var papers = m_Book.papers;
                foreach (var paper in papers)
                {
                    UpdatePaperPosition(paper);
                }

                UpdateBindingVertices();

                foreach (var paper in papers)
                {
                    paper.UpdateMesh();
                }

                UpdateRootPosition();
            }

            void UpdateBindingVertices()
            {
                var papers = m_Book.papers;
                Vector3 stapleDirection = Vector3.zero;
                Vector3 bindingNormal = Vector3.zero;

                float bindingRadius = m_BindingRadius * 0.6f;
                float stapleThickness = m_StapleThickness * 0.5f;
                float coverThickness = papers[0].thickness;

                Matrix4x4 rootLocalToWorldMatrix = m_Root.localToWorldMatrix;

                int paperCount = papers.Length;

                for (int i = 0; i < paperCount; i++)
                {
                    Paper paper = papers[i];
                    Transform transform = paper.transform;
                    PaperPattern pattern = paper.meshData.pattern as PaperPattern;
                    Vector3[] baseVertices = paper.meshData.baseVertices;
                    float[] baseXArray = pattern.baseXArray;
                    float[] baseZArray = pattern.baseZArray;
                    int nX = baseXArray.Length;
                    int nZ = baseZArray.Length;
                    int lastXIndex = m_BindingVertexCount + 1;
                    float thickness = paper.thickness;
                    int[] xNoneSeamIndexes = pattern.xNoneSeamIndexes;
                    Vector3 localPosition = transform.localPosition;
                    int sheetIndex = i;
                    float bindingNormalMul = stapleThickness;
                    if (i >= paperCount / 2)
                    {
                        sheetIndex = paperCount - i - 1;
                        bindingNormalMul *= -1;
                    }
                    float stapleDirectionMul = (coverThickness + thickness) * 0.5f + thickness * (sheetIndex - 1);

                    Matrix4x4 rootLocal2PaperLocalMatrix = transform.worldToLocalMatrix * rootLocalToWorldMatrix;

                    float sheetTime = sheetIndex / (paperCount / 2f);
                    bindingRadius = m_BindingRadius * Mathf.Lerp(0.45f, 0.65f, 1 - sheetTime);

                    for (int iz = 0; iz < nZ; iz++)
                    {
                        float z = baseZArray[iz];

                        Vector3 a = new Vector3(0, 0, localPosition.z + z);
                        Vector3 c = localPosition; c.z += z;
                        Vector3 b = c + paper.GetDirection(z) * bindingRadius;

                        if (i == 0 && iz == 0)
                        {
                            var paper2 = papers[paperCount - 1];
                            Vector3 localPosition2 = paper2.transform.localPosition;
                            Vector3 c2 = localPosition2; c2.z += z;
                            Vector3 b2 = c2 + paper2.GetDirection(z) * bindingRadius;
                            bindingNormal = (b - b2).normalized;
                            stapleDirection = -new Vector3(-bindingNormal.y, bindingNormal.x, 0).normalized;
                            float qz = Mathf.Atan2(stapleDirection.y, stapleDirection.x) * Mathf.Rad2Deg;
                            m_StapleRenderer.transform.localEulerAngles = new Vector3(0, 0, qz);
                        }

                        if (sheetIndex > 0)
                        {
                            a += stapleDirection * stapleDirectionMul;
                        }

                        int jz = iz * nX;

                        baseVertices[jz] = rootLocal2PaperLocalMatrix.MultiplyPoint3x4(a);

                        a += bindingNormal * bindingNormalMul;

                        a = rootLocal2PaperLocalMatrix.MultiplyPoint3x4(a);
                        b = rootLocal2PaperLocalMatrix.MultiplyPoint3x4(b);
                        c = rootLocal2PaperLocalMatrix.MultiplyPoint3x4(c);

                        baseVertices[jz + xNoneSeamIndexes[1]] = a;

                        for (int ix = 2; ix < lastXIndex; ix++)
                        {
                            float t = Mathf.InverseLerp(1, lastXIndex, ix);
                            baseVertices[jz + xNoneSeamIndexes[ix]] = BezierUtility.Evaluate(a, b, c, t);
                        }

                        /*
                        if (leftSeam.active)
                        {
                            for (int ix = lastXIndex; ix >= leftSeam.index; ix--)
                            {
                                int iv = jz + ix;
                                baseVertices[iv] = baseVertices[iv - 1];
                            }
                        }*/
                    }
                }
            }
        }
    }

    [System.Serializable]
    class StapleSetup
    {
        const float kMinThickness = 0.01f;
        const float kMaxThickness = 0.1f;
        const float kMinCrown = 0.04f;
        const float kMaxCrown = 0.4f;
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

        [Tooltip("The size of the top portion of the staple.")]
        [SerializeField, Range(kMinCrown, kMaxCrown)]
        float m_Crown;

        [Tooltip("The blank space between the staples and the book's horizontal edges.")]
        [SerializeField, Range(0, 1)]
        float m_Margin;

        [Tooltip("The number of staples.")]
        [SerializeField, Range(kMinCount, kMaxCount)]
        int m_Count;

        [Tooltip("The quality level of the staples mesh.")]
        [SerializeField, Range(kMinQuality, kMaxQuality)]
        int m_Quality;


        public StapleSetup()
        {
            color = Color.white;
            thickness = 0.05f;
            crown = 0.2f;
            margin = 0.1f;
            count = 4;
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
            set => m_Thickness = Mathf.Clamp(value, kMinThickness, kMaxThickness);
        }

        public float margin
        {
            get => m_Margin;
            set => m_Margin = Mathf.Clamp01(value);
        }

        public float crown
        {
            get => m_Crown;
            set => m_Crown = Mathf.Clamp(value, kMinCrown, kMaxCrown);
        }

        public int count
        {
            get => m_Count;
            set => m_Count = Mathf.Clamp(value, kMinCount, kMaxCount);
        }

        public int quality
        {
            get => m_Quality;
            set => m_Quality = Mathf.Clamp(value, kMinQuality, kMaxQuality);
        }
    }
}